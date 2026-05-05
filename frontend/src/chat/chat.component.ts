import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { SvgIconComponent } from 'angular-svg-icon';
import { ChatBubbleComponent } from '../../components/chat-bubble/chat-bubble.component';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';

@Component({
  selector: 'app-chat',
  imports: [CommonModule, SvgIconComponent, ChatBubbleComponent],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss',
})
export class ChatComponent {
  public sharedSvgRoutes = SharedSvgRoutes;
  public isGroupsExpanded = true;
  public isDirectExpanded = true;
  public readonly currentUserId = Number(localStorage.getItem('user_id') ?? 0);

  public readonly groups = [
    { id: 1, name: 'carrier' },
    { id: 2, name: 'release' },
  ];

  public readonly directUsers = [
    { id: 2, firstName: 'Pera', lastName: 'Peric' },
    { id: 3, firstName: 'Mika', lastName: 'Mikic' },
  ];

  public selectedDirectUser = this.directUsers[0];

  public onHeaderGroupsToggle(): void {
    this.isGroupsExpanded = !this.isGroupsExpanded;
  }

  public onHeaderDirectToggle(): void {
    this.isDirectExpanded = !this.isDirectExpanded;
  }

  public onDirectUserSelect(userId: number): void {
    const selected = this.directUsers.find((user) => user.id === userId);
    if (!selected) {
      return;
    }

    this.selectedDirectUser = selected;
  }
}
