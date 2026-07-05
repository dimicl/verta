import { Component, Input, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbActiveModal, NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { SvgIconComponent } from 'angular-svg-icon';
import { DropZoneComponent } from '../drop-zone/drop-zone.component';
import { TaskStatus } from '../../shared/types/task-status.type';
import { TaskPriority } from '../../shared/types/task-priority.type';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { VeStatusComponent } from '../ve-status/ve-status.component';
import {
  TaskAssigneeComponent,
  TaskMember,
} from '../task-assignee/task-assignee.component';
import { TaskCommentsComponent } from '../task-comments/task-comments.component';
import { TaskPriorityComponent } from '../task-priority/task-priority.component';

@Component({
  selector: 'app-subtask-modal',
  imports: [
    SvgIconComponent,
    NgbModule,
    CommonModule,
    FormsModule,
    DropZoneComponent,
    VeStatusComponent,
    TaskAssigneeComponent,
    TaskCommentsComponent,
    TaskPriorityComponent,
  ],
  templateUrl: './subtask-modal.component.html',
  styleUrl: './subtask-modal.component.scss',
})
export class SubtaskModalComponent {
  public sharedSvgRoutes = SharedSvgRoutes;

  @Input() public isEditMode = false;
  @Input() public parentWorkItemId: number | null = null;
  @Input() public parentTaskTitle = '';
  @Input() public subWorkItemId: number | null = null;
  @Input() public title = '';
  @Input() public description = '';
  @Input() public status: TaskStatus = 'ToDo';
  @Input() public priority: TaskPriority = 'Medium';
  @Input() public points: number | null = null;
  @Input() public members: TaskMember[] = [];
  @Input() public assignedUserId: number | null = null;
  @Input() public currentUserId = 0;
  @Input() public currentUserFirstName = 'User';
  @Input() public currentUserLastName = '';

  @ViewChild(TaskCommentsComponent)
  private taskComments?: TaskCommentsComponent;

  @ViewChild(DropZoneComponent)
  public dropZone?: DropZoneComponent;

  constructor(private activeModal: NgbActiveModal) {}

  public onClose(): void {
    this.activeModal.dismiss();
  }

  public onSave(): void {
    this.activeModal.close({
      title: this.title,
      description: this.description,
      comments: this.taskComments?.pendingComments ?? [],
      pendingFiles: this.dropZone?.pendingFiles ?? [],
      status: this.isEditMode ? this.status : 'ToDo',
      priority: this.priority,
      points: this.points,
      assignedUserId: this.assignedUserId,
    });
  }
}
