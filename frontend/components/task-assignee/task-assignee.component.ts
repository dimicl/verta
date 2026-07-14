import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NgbModule, NgbPopover } from '@ng-bootstrap/ng-bootstrap';
import { AvatarComponent } from '../avatar/avatar.component';

export interface TaskMember {
  id: number;
  firstName: string;
  lastName: string;
}

@Component({
  selector: 'app-task-assignee',
  imports: [CommonModule, NgbModule, AvatarComponent],
  templateUrl: './task-assignee.component.html',
  styleUrl: './task-assignee.component.scss',
})
export class TaskAssigneeComponent {
  @Input() public members: TaskMember[] = [];
  @Input() public assignedUserId: number | null = null;
  @Input() public disabled = false;
  @Output() public assignedUserIdChange = new EventEmitter<number | null>();

  public get assigneeLabel(): string {
    if (!this.assignedUserId) {
      return 'Unassigned';
    }

    const member = this.members.find((m) => m.id === this.assignedUserId);
    if (!member) {
      return 'Unassigned';
    }

    return `${member.firstName} ${member.lastName}`.trim();
  }

  public get assignedMember(): TaskMember | null {
    if (!this.assignedUserId) {
      return null;
    }

    return this.members.find((m) => m.id === this.assignedUserId) ?? null;
  }

  public onSelectMember(
    userId: number | null,
    popover: NgbPopover
  ): void {
    if (this.disabled) {
      return;
    }
    this.assignedUserId = userId;
    this.assignedUserIdChange.emit(userId);
    popover.close();
  }
}
