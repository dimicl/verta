import { CommonModule } from '@angular/common';
import {
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  OnChanges,
  OnInit,
  Output,
  SimpleChanges,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SvgIconComponent } from 'angular-svg-icon';
import { forkJoin } from 'rxjs';
import { VeStatusComponent } from '../ve-status/ve-status.component';
import { SubWorkItemResponse } from '../../shared/interfaces';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import {
  CommentService,
  SubWorkItemService,
  WorkItemFileService,
} from '../../shared/services';
import { TaskStatus } from '../../shared/types/task-status.type';
import { TaskMember } from '../task-assignee/task-assignee.component';
import { TaskPriority } from '../../shared/types/task-priority.type';

@Component({
  selector: 'app-task-subtasks',
  imports: [CommonModule, SvgIconComponent, VeStatusComponent],
  templateUrl: './task-subtasks.component.html',
  styleUrl: './task-subtasks.component.scss',
})
export class TaskSubtasksComponent implements OnInit, OnChanges {
  @Input() public workItemId: number | null = null;
  @Input() public parentTaskTitle = '';
  @Input() public members: TaskMember[] = [];
  @Input() public currentUserId = 0;
  @Input() public currentUserFirstName = 'User';
  @Input() public currentUserLastName = '';
  @Output() public subtasksChanged = new EventEmitter<SubWorkItemResponse[]>();
  @Output() public createSubtaskRequest = new EventEmitter<void>();
  @Output() public editSubtaskRequest = new EventEmitter<SubWorkItemResponse>();

  public readonly sharedSvgRoutes = SharedSvgRoutes;
  public readonly subtasks = signal<SubWorkItemResponse[]>([]);
  public readonly isLoading = signal(false);
  public readonly isSubmitting = signal(false);
  public readonly errorMessage = signal('');

  private readonly subWorkItemService = inject(SubWorkItemService);
  private readonly commentService = inject(CommentService);
  private readonly workItemFileService = inject(WorkItemFileService);
  private readonly destroyRef = inject(DestroyRef);

  public ngOnInit(): void {
    this.loadSubtasks();
  }

  public ngOnChanges(changes: SimpleChanges): void {
    if (changes['workItemId'] && !changes['workItemId'].firstChange) {
      this.loadSubtasks();
    }
  }

  public onCreateSubtask(): void {
    if (!this.workItemId) {
      return;
    }

    this.createSubtaskRequest.emit();
  }

  public onOpenSubtask(subtask: SubWorkItemResponse): void {
    this.editSubtaskRequest.emit(subtask);
  }

  public onStatusChange(
    subtask: SubWorkItemResponse,
    status: TaskStatus
  ): void {
    if (subtask.status === status || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    this.subWorkItemService
      .changeStatus(subtask.id, status)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.updateLocalSubtask(updated);
          this.isSubmitting.set(false);
        },
        error: (err) => {
          this.errorMessage.set(
            err.error?.message ?? 'Failed to change subtask status.'
          );
          this.isSubmitting.set(false);
        },
      });
  }

  public saveFromModal(
    existing: SubWorkItemResponse | null,
    result: {
      title: string;
      description: string;
      comments?: string[];
      pendingFiles?: File[];
      status: TaskStatus;
      priority: TaskPriority;
      assignedUserId?: number | null;
    }
  ): void {
    this.saveSubtask(existing, result);
  }

  private saveSubtask(
    existing: SubWorkItemResponse | null,
    result: {
      title: string;
      description: string;
      comments?: string[];
      pendingFiles?: File[];
      status: TaskStatus;
      priority: TaskPriority;
      assignedUserId?: number | null;
    }
  ): void {
    if (!this.workItemId) {
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    const request = {
      name: result.title.trim(),
      description: result.description.trim(),
      assignedUserId: result.assignedUserId,
      priority: result.priority,
    };

    const save$ = existing
      ? this.subWorkItemService.update(existing.id, request)
      : this.subWorkItemService.create({
          ...request,
          workItemId: this.workItemId,
        });

    save$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (saved) => {
        const finalize = (item: SubWorkItemResponse) => {
          if (existing) {
            this.updateLocalSubtask(item);
          } else {
            this.subtasks.update((items) => [...items, item]);
            this.subtasksChanged.emit(this.subtasks());
          }

          this.isSubmitting.set(false);
          this.applyPostSaveActions(item, result);
        };

        if (!existing && result.status !== 'ToDo') {
          this.subWorkItemService.changeStatus(saved.id, result.status).subscribe({
            next: (updated) => finalize(updated),
            error: () => finalize(saved),
          });
          return;
        }

        if (existing && result.status !== existing.status) {
          this.subWorkItemService.changeStatus(saved.id, result.status).subscribe({
            next: (updated) => finalize(updated),
            error: () => finalize(saved),
          });
          return;
        }

        finalize(saved);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message ?? 'Failed to save subtask.');
        this.isSubmitting.set(false);
      },
    });
  }

  private applyPostSaveActions(
    subtask: SubWorkItemResponse,
    result: {
      comments?: string[];
      pendingFiles?: File[];
    }
  ): void {
    const comments = (result.comments ?? [])
      .map((content) => content.trim())
      .filter((content) => content.length > 0);
    const pendingFiles = result.pendingFiles ?? [];

    if (!this.workItemId || (comments.length === 0 && pendingFiles.length === 0)) {
      this.loadSubtasks();
      return;
    }

    const actions = [
      ...comments.map((content) =>
        this.commentService.create({
          workItemId: this.workItemId!,
          subWorkItemId: subtask.id,
          content,
        })
      ),
      ...pendingFiles.map((file) =>
        this.workItemFileService.upload(this.workItemId!, file, subtask.id)
      ),
    ];

    forkJoin(actions)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadSubtasks(),
        error: () => this.loadSubtasks(),
      });
  }

  private updateLocalSubtask(updated: SubWorkItemResponse): void {
    this.subtasks.update((items) =>
      items.map((item) => (item.id === updated.id ? updated : item))
    );
    this.subtasksChanged.emit(this.subtasks());
  }

  private loadSubtasks(): void {
    if (!this.workItemId) {
      this.subtasks.set([]);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.subWorkItemService
      .getByWorkItemId(this.workItemId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.subtasks.set(items);
          this.subtasksChanged.emit(items);
          this.isLoading.set(false);
        },
        error: (err) => {
          this.errorMessage.set(
            err.error?.message ?? 'Failed to load subtasks.'
          );
          this.isLoading.set(false);
        },
      });
  }
}
