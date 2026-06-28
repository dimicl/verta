import { Component, EventEmitter, Input, Output } from '@angular/core';
import { SvgIconComponent } from 'angular-svg-icon';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { TaskStatus } from '../../shared/types';

@Component({
  selector: 'app-ve-status',
  imports: [SvgIconComponent],
  templateUrl: './ve-status.component.html',
  styleUrl: './ve-status.component.scss',
})
export class VeStatusComponent {
  public sharedSvgRoutes = SharedSvgRoutes;

  @Input() public taskStatus: TaskStatus = 'ToDo';
  @Output() public statusChange = new EventEmitter<TaskStatus>();

  public statusOrder: TaskStatus[] = [
    'ToDo',
    'InProgress',
    'PR',
    'Testing',
    'Done',
  ];

  public onToggleStatus(): void {
    const currentIndex = this.statusOrder.indexOf(this.taskStatus);
    const nextIndex = (currentIndex + 1) % this.statusOrder.length;
    this.taskStatus = this.statusOrder[nextIndex];
    this.statusChange.emit(this.taskStatus);
  }
}
