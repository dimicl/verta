import { Component } from '@angular/core';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { CommonModule } from '@angular/common';
import { NgbActiveModal, NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { SvgIconComponent } from 'angular-svg-icon';
import { DropZoneComponent } from '../drop-zone/drop-zone.component';

@Component({
  selector: 'app-task-modal',
  imports: [SvgIconComponent, NgbModule, CommonModule, DropZoneComponent],
  templateUrl: './task-modal.component.html',
  styleUrl: './task-modal.component.scss',
})
export class TaskModalComponent {
  public sharedSvgRoutes = SharedSvgRoutes;
  public isEditMode = false;

  constructor(private activeModal: NgbActiveModal) {}

  public onClose(): void {
    this.activeModal.dismiss();
  }
}
