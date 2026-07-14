import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NgbModule, NgbPopover } from '@ng-bootstrap/ng-bootstrap';
import { SvgIconComponent } from 'angular-svg-icon';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { getAllowedNextStatuses } from '../../shared/helpers/task-status.helper';
import { TaskStatus } from '../../shared/types';

@Component({
  selector: 'app-ve-status',
  imports: [SvgIconComponent, CommonModule, NgbModule],
  templateUrl: './ve-status.component.html',
  styleUrl: './ve-status.component.scss',
})
export class VeStatusComponent {
  public sharedSvgRoutes = SharedSvgRoutes;

  @Input() public taskStatus: TaskStatus = 'ToDo';
  @Input() public disabled = false;
  @Output() public statusChange = new EventEmitter<TaskStatus>();

  public get allowedStatuses(): TaskStatus[] {
    return getAllowedNextStatuses(this.taskStatus);
  }

  public onSelectStatus(status: TaskStatus, popover: NgbPopover): void {
    if (this.disabled) {
      return;
    }

    if (status === this.taskStatus) {
      popover.close();
      return;
    }

    this.taskStatus = status;
    this.statusChange.emit(status);
    popover.close();
  }
}
