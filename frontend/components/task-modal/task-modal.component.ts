import { Component, Input, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbActiveModal, NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { SvgIconComponent } from 'angular-svg-icon';
import { DropZoneComponent } from '../drop-zone/drop-zone.component';
import { TaskStatus } from '../../shared/types/task-status.type';
import { SprintResponse } from '../../shared/interfaces';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { VeStatusComponent } from '../ve-status/ve-status.component';
import {
  TaskAssigneeComponent,
  TaskMember,
} from '../task-assignee/task-assignee.component';
import { TaskCommentsComponent } from '../task-comments/task-comments.component';

@Component({
  selector: 'app-task-modal',
  imports: [
    SvgIconComponent,
    NgbModule,
    CommonModule,
    FormsModule,
    DropZoneComponent,
    VeStatusComponent,
    TaskAssigneeComponent,
    TaskCommentsComponent,
  ],
  templateUrl: './task-modal.component.html',
  styleUrl: './task-modal.component.scss',
})
export class TaskModalComponent {
  public sharedSvgRoutes = SharedSvgRoutes;

  @Input() public isEditMode = false;
  @Input() public workItemId: number | null = null;
  @Input() public title = '';
  @Input() public description = '';
  @Input() public status: TaskStatus = 'ToDo';
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
      points: this.points,
      sprintId: this.sprintId,
      assignedUserId: this.assignedUserId,
    });
  }
}
