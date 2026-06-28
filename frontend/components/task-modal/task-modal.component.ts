import { Component, Input } from '@angular/core';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbActiveModal, NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { SvgIconComponent } from 'angular-svg-icon';
import { DropZoneComponent } from '../drop-zone/drop-zone.component';
import { TaskStatus } from '../../shared/types/task-status.type';

@Component({
  selector: 'app-task-modal',
  imports: [SvgIconComponent, NgbModule, CommonModule, FormsModule, DropZoneComponent],
  templateUrl: './task-modal.component.html',
  styleUrl: './task-modal.component.scss',
})
export class TaskModalComponent {
  public sharedSvgRoutes = SharedSvgRoutes;
  @Input() public isEditMode = false;
  @Input() public title = '';
  @Input() public description = '';
  @Input() public comment = '';
  @Input() public status: TaskStatus = 'ToDo';
  @Input() public points: number | null = null;

  public statusOptions: TaskStatus[] = [
    'ToDo',
    'InProgress',
    'PR',
    'Testing',
    'Done',
  ];

  constructor(private activeModal: NgbActiveModal) {}

  public onClose(): void {
    this.activeModal.dismiss();
  }

  public onSave(): void {
    this.activeModal.close({
      title: this.title,
      description: this.description,
      comment: this.comment,
      status: this.status,
      points: this.points,
    });
  }
}
