import {
  ChangeDetectorRef,
  Component,
  Input,
  OnDestroy,
  ViewChild,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbActiveModal, NgbModal, NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { SvgIconComponent } from 'angular-svg-icon';
import { DropZoneComponent } from '../drop-zone/drop-zone.component';
import { TaskStatus } from '../../shared/types/task-status.type';
import { TaskPriority } from '../../shared/types/task-priority.type';
import { SprintResponse, SubWorkItemResponse } from '../../shared/interfaces';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import {
  CONFIRM_MODAL_OPTIONS,
  STACKED_MODAL_OPTIONS,
} from '../../shared/constants/modal-options';
import { SubWorkItemService, TaskService } from '../../shared/services';
import { ConfirmModalComponent } from '../confirm-modal/confirm-modal.component';
import { VeStatusComponent } from '../ve-status/ve-status.component';
import {
  TaskAssigneeComponent,
  TaskMember,
} from '../task-assignee/task-assignee.component';
import { TaskCommentsComponent } from '../task-comments/task-comments.component';
import { TaskSubtasksComponent } from '../task-subtasks/task-subtasks.component';
import { TaskPriorityComponent } from '../task-priority/task-priority.component';

@Component({
  selector: 'app-task-modal',
  standalone: true,
  imports: [
    SvgIconComponent,
    NgbModule,
    CommonModule,
    FormsModule,
    DropZoneComponent,
    VeStatusComponent,
    TaskAssigneeComponent,
    TaskCommentsComponent,
    TaskSubtasksComponent,
    TaskPriorityComponent,
  ],
  templateUrl: './task-modal.component.html',
  styleUrl: './task-modal.component.scss',
})
export class TaskModalComponent implements OnDestroy {
  public sharedSvgRoutes = SharedSvgRoutes;

  @Input() public isSubtask = false;
  @Input() public isEditMode = false;
  @Input() public isReadOnly = false;
  @Input() public workItemId: number | null = null;
  @Input() public boardId: number | null = null;
  @Input() public parentWorkItemId: number | null = null;
  @Input() public parentTaskTitle = '';
  @Input() public subWorkItemId: number | null = null;
  @Input() public title = '';
  @Input() public description = '';
  @Input() public status: TaskStatus = 'ToDo';
  @Input() public priority: TaskPriority = 'Medium';
  @Input() public points: number | null = null;
  @Input() public sprints: SprintResponse[] = [];
  @Input() public sprintId: number | null = null;
  @Input() public members: TaskMember[] = [];
  @Input() public assignedUserId: number | null = null;
  @Input() public currentUserId = 0;
  @Input() public currentUserFirstName = 'User';
  @Input() public currentUserLastName = '';

  @ViewChild(TaskCommentsComponent)
  private taskComments?: TaskCommentsComponent;

  @ViewChild(TaskSubtasksComponent)
  private taskSubtasks?: TaskSubtasksComponent;

  @ViewChild(DropZoneComponent)
  public dropZone?: DropZoneComponent;

  public subtasksChanged: SubWorkItemResponse[] = [];
  public errorMessage = '';

  private readonly modalService = inject(NgbModal);
  private readonly subWorkItemService = inject(SubWorkItemService);
  private readonly taskService = inject(TaskService);
  private readonly cdr = inject(ChangeDetectorRef);
  private fieldSaveTimer: ReturnType<typeof setTimeout> | null = null;

  constructor(private activeModal: NgbActiveModal) {}

  public ngOnDestroy(): void {
    if (this.fieldSaveTimer) {
      clearTimeout(this.fieldSaveTimer);
    }
  }

  public get modalTitle(): string {
    if (this.isReadOnly) {
      return this.isSubtask ? 'View Subtask' : 'View Task';
    }

    if (this.isSubtask) {
      return this.isEditMode ? 'Edit Subtask' : 'Add Subtask';
    }

    return this.isEditMode ? 'Edit Task' : 'Add Task';
  }

  public get saveLabel(): string {
    if (this.isSubtask) {
      return this.isEditMode ? 'Save changes' : 'Create subtask';
    }

    return this.isEditMode ? 'Save changes' : 'Create task';
  }

  public get commentsWorkItemId(): number | null {
    return this.isSubtask ? this.parentWorkItemId : this.workItemId;
  }

  public get commentsSubWorkItemId(): number | null | undefined {
    return this.isSubtask ? this.subWorkItemId : undefined;
  }

  public onSubtasksChanged(subtasks: SubWorkItemResponse[]): void {
    this.subtasksChanged = subtasks;
  }

  public onCreateSubtask(): void {
    if (this.isReadOnly) {
      return;
    }
    this.openSubtaskModal();
  }

  public onEditSubtask(subtask: SubWorkItemResponse): void {
    if (this.isReadOnly) {
      return;
    }
    this.openSubtaskModal(subtask);
  }

  public openSubtaskModal(existing?: SubWorkItemResponse): void {
    if (!this.workItemId) {
      return;
    }

    const modalRef = this.modalService.open(
      TaskModalComponent,
      STACKED_MODAL_OPTIONS
    );
    const instance = modalRef.componentInstance;
    instance.isSubtask = true;
    instance.isEditMode = !!existing;
    instance.isReadOnly = this.isReadOnly;
    instance.parentWorkItemId = this.workItemId;
    instance.parentTaskTitle = this.title;
    instance.boardId = this.boardId;
    instance.subWorkItemId = existing?.id ?? null;
    instance.title = existing?.name ?? '';
    instance.description = existing?.description ?? '';
    instance.status = existing?.status ?? 'ToDo';
    instance.priority = existing?.priority ?? 'Medium';
    instance.assignedUserId = existing?.assignedUserId ?? null;
    instance.members = this.members;
    instance.currentUserId = this.currentUserId;
    instance.currentUserFirstName = this.currentUserFirstName;
    instance.currentUserLastName = this.currentUserLastName;

    modalRef.result.then(
      (result) => {
        if (result && !this.isReadOnly) {
          this.taskSubtasks?.saveFromModal(existing ?? null, result);
        }
      },
      () => {}
    );
  }

  public onDeleteSubtask(): void {
    if (this.isReadOnly || !this.isSubtask || !this.isEditMode || !this.subWorkItemId) {
      return;
    }

    const confirmRef = this.modalService.open(
      ConfirmModalComponent,
      CONFIRM_MODAL_OPTIONS
    );
    confirmRef.componentInstance.title = 'Delete subtask';
    confirmRef.componentInstance.message =
      'Želite li sigurno da obrišete ovaj subtask?';

    confirmRef.result.then(
      (confirmed) => {
        if (confirmed) {
          this.deleteSubtask();
        }
      },
      () => {}
    );
  }

  private deleteSubtask(): void {
    if (!this.subWorkItemId) {
      return;
    }

    this.subWorkItemService.delete(this.subWorkItemId).subscribe({
      next: () => {
        this.activeModal.close({ deleted: true, id: this.subWorkItemId });
      },
      error: (err) => {
        this.errorMessage = err.error?.message ?? 'Failed to delete subtask.';
      },
    });
  }

  public onClose(): void {
    this.activeModal.dismiss();
  }

  public onTitleOrDescriptionChange(): void {
    this.scheduleFieldAutosave();
  }

  public onStatusChange(status: TaskStatus): void {
    this.status = status;
    if (!this.canAutosaveWorkItem()) {
      return;
    }

    this.taskService.changeStatus(this.workItemId!, status).subscribe({
      error: (err) => {
        this.errorMessage = err.error?.message ?? 'Failed to change status.';
        this.cdr.detectChanges();
      },
    });
  }

  public onAssigneeChange(userId: number | null): void {
    this.assignedUserId = userId;
    if (!this.canAutosaveWorkItem()) {
      return;
    }

    this.taskService.changeAssignee(this.workItemId!, userId).subscribe({
      error: (err) => {
        this.errorMessage = err.error?.message ?? 'Failed to change assignee.';
        this.cdr.detectChanges();
      },
    });
  }

  public onSave(): void {
    if (this.isReadOnly) {
      this.activeModal.dismiss();
      return;
    }

    if (this.fieldSaveTimer) {
      clearTimeout(this.fieldSaveTimer);
      this.fieldSaveTimer = null;
    }

    this.activeModal.close({
      isSubtask: this.isSubtask,
      title: this.title,
      description: this.description,
      comments: this.taskComments?.pendingComments ?? [],
      pendingFiles: this.dropZone?.pendingFiles ?? [],
      status: this.isEditMode ? this.status : 'ToDo',
      priority: this.priority,
      points: this.points,
      sprintId: this.sprintId,
      assignedUserId: this.assignedUserId,
    });
  }

  public applyRemoteFields(update: {
    title?: string;
    description?: string;
    status?: TaskStatus;
    assignedUserId?: number | null;
    sprintId?: number | null;
  }): void {
    if (update.title !== undefined) {
      this.title = update.title;
    }
    if (update.description !== undefined) {
      this.description = update.description;
    }
    if (update.status !== undefined) {
      this.status = update.status;
    }
    if (update.assignedUserId !== undefined) {
      this.assignedUserId = update.assignedUserId;
    }
    if (update.sprintId !== undefined) {
      this.sprintId = update.sprintId;
    }
    this.cdr.detectChanges();
  }

  public reloadLiveContent(): void {
    this.taskComments?.reload();
    this.taskSubtasks?.reload();
    this.dropZone?.reload();
  }

  public grantWriteAccess(): void {
    this.isReadOnly = false;
    this.cdr.detectChanges();
  }

  private canAutosaveWorkItem(): boolean {
    return (
      this.isEditMode &&
      !this.isReadOnly &&
      !this.isSubtask &&
      !!this.workItemId &&
      !!this.boardId
    );
  }

  private scheduleFieldAutosave(): void {
    if (!this.canAutosaveWorkItem()) {
      return;
    }

    if (this.fieldSaveTimer) {
      clearTimeout(this.fieldSaveTimer);
    }

    this.fieldSaveTimer = setTimeout(() => {
      this.fieldSaveTimer = null;
      this.persistWorkItemFields();
    }, 500);
  }

  private persistWorkItemFields(): void {
    if (!this.canAutosaveWorkItem()) {
      return;
    }

    const name = this.title.trim();
    const description = this.description.trim();
    if (!name || !description) {
      return;
    }

    this.taskService
      .update(this.workItemId!, {
        name,
        description,
        boardId: this.boardId!,
        sprintId: this.sprintId ?? undefined,
        priority: this.priority,
        assignedUserId: this.assignedUserId,
      })
      .subscribe({
        error: (err) => {
          this.errorMessage = err.error?.message ?? 'Failed to save changes.';
          this.cdr.detectChanges();
        },
      });
  }
}
