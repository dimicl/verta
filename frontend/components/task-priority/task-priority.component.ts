import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NgbModule, NgbPopover } from '@ng-bootstrap/ng-bootstrap';
import { TASK_PRIORITIES, normalizeTaskPriority } from '../../shared/helpers/task-priority.helper';
import { TaskPriority } from '../../shared/types/task-priority.type';

@Component({
  selector: 'app-task-priority',
  imports: [CommonModule, NgbModule],
  templateUrl: './task-priority.component.html',
  styleUrl: './task-priority.component.scss',
})
export class TaskPriorityComponent {
  @Input() public priority: string = 'Medium';
  @Output() public priorityChange = new EventEmitter<TaskPriority>();

  public readonly priorities = TASK_PRIORITIES;

  public get normalizedPriority(): TaskPriority {
    return normalizeTaskPriority(this.priority);
  }

  public onSelectPriority(priority: TaskPriority, popover: NgbPopover): void {
    if (priority === this.normalizedPriority) {
      popover.close();
      return;
    }

    this.priority = priority;
    this.priorityChange.emit(priority);
    popover.close();
  }
}
