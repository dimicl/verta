import {
  Component,
  EventEmitter,
  Input,
  Output,
  WritableSignal,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { InputComponent } from '../../../../components/input/input.component';
import { SharedSvgRoutes } from '../../../../shared/constants/shared-svg-routes';
import { SvgIconComponent } from 'angular-svg-icon';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { AvatarComponent } from '../../../../components/avatar/avatar.component';
import { VeNotificationComponent } from '../../../../components/ve-notification/ve-notification.component';
import { VeExtraMembersComponent } from '../../../../components/ve-extra-members/ve-extra-members.component';
import { VeProfileComponent } from '../../../../components/ve-profile/ve-profile.component';
import { BoardWorkspaceStore } from '../../services/board-workspace.store';

type ToolbarTab = 'Backlog' | 'Board' | 'Chat';

@Component({
  selector: 'app-board-header',
  templateUrl: './board-header.component.html',
  styleUrl: './board-header.component.scss',
  imports: [
    CommonModule,
    InputComponent,
    SvgIconComponent,
    NgbModule,
    AvatarComponent,
    VeNotificationComponent,
    VeExtraMembersComponent,
    VeProfileComponent,
  ],
})
export class BoardHeaderComponent {
  readonly store = inject(BoardWorkspaceStore);
  readonly sharedSvgRoutes = SharedSvgRoutes;

  readonly toolbarItems = ['Backlog', 'Board', 'Chat'] as const;

  @Input({ required: true }) selectedToolbarTab!: WritableSignal<ToolbarTab>;

  @Output() toolbarTabSelect = new EventEmitter<ToolbarTab>();
  @Output() searchChange = new EventEmitter<string>();
  @Output() addSprint = new EventEmitter<void>();
  @Output() inviteBoard = new EventEmitter<void>();
  @Output() addTask = new EventEmitter<void>();
  @Output() userSelect = new EventEmitter<number>();

  getToolbarSliderLeft(): string {
    const selectedIndex = this.toolbarItems.indexOf(
      this.selectedToolbarTab()
    );
    const safeIndex = selectedIndex < 0 ? 0 : selectedIndex;
    const tabWidth = 100 / this.toolbarItems.length;

    return `${safeIndex * tabWidth}%`;
  }

  getToolbarSliderWidth(): string {
    return `${100 / this.toolbarItems.length}%`;
  }
}
