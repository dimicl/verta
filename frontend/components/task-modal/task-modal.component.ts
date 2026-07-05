import { Component, Input, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbActiveModal, NgbModal, NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { SvgIconComponent } from 'angular-svg-icon';
import { DropZoneComponent } from '../drop-zone/drop-zone.component';
import { TaskStatus } from '../../shared/types/task-status.type';
import { TaskPriority } from '../../shared/types/task-priority.type';
import { SprintResponse, SubWorkItemResponse } from '../../shared/interfaces';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { STACKED_MODAL_OPTIONS } from '../../shared/constants/modal-options';
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
export class TaskModalComponent {
  public sharedSvgRoutes = SharedSvgRoutes;

  @Input() public isSubtask = false;
  @Input() public isEditMode = false;
  @Input() public workItemId: number | null = null;
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

  private readonly modalService = inject(NgbModal);

  constructor(private activeModal: NgbActiveModal) {}

  public get modalTitle(): string {
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
    this.openSubtaskModal();
  }

  public onEditSubtask(subtask: SubWorkItemResponse): void {
    this.openSubtaskModal(subtask);
  }

  public openSubtaskModal(existing?: SubWorkItemResponse): void {
    if (!this.workItemId) {
      return;
    }

    const modalRef = this.modalService.open(TaskModalComponent, STACKED_MODAL_OPTIONS);
    const instance = modalRef.componentInstance;

    instance.isSubtask = true;
    instance.isEditMode = !!existing;
    instance.parentWorkItemId = this.workItemId;
    instance.parentTaskTitle = this.title;
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
        if (result?.title) {
          this.taskSubtasks?.saveFromModal(existing ?? null, result);
        }
      },
      () => {}
    );
  }

  public onClose(): void {
    this.activeModal.dismiss();
  }

  public onSave(): void {
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
}
