import { CommonModule } from '@angular/common';
import { Component, Input, output, signal } from '@angular/core';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-ve-extra-members',
  templateUrl: './ve-extra-members.component.html',
  styleUrl: './ve-extra-members.component.scss',
  imports: [CommonModule, NgbModule],
})
export class VeExtraMembersComponent {
  @Input() members: Array<{
    id: number;
    firstName: string;
    lastName: string;
  }> = [];

  @Input() extraCount = 0;
  @Input() selectedUserId: number | null = null;

  memberSelect = output<number>();
  public onNotificationShown = signal<boolean>(false);

  public getMemberName(member: {
    firstName: string;
    lastName: string;
  }): string {
    return `${member.firstName} ${member.lastName}`.trim();
  }
}
