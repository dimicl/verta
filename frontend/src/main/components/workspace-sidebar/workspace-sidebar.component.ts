import { Component, EventEmitter, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SvgIconComponent } from 'angular-svg-icon';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { SharedSvgRoutes } from '../../../../shared/constants/shared-svg-routes';
import { BoardResponse } from '../../../../shared/interfaces';
import { BoardWorkspaceStore } from '../../services/board-workspace.store';

@Component({
  selector: 'app-workspace-sidebar',
  templateUrl: './workspace-sidebar.component.html',
  styleUrl: './workspace-sidebar.component.scss',
  imports: [CommonModule, SvgIconComponent, NgbModule],
})
export class WorkspaceSidebarComponent {
  readonly store = inject(BoardWorkspaceStore);
  readonly sharedSvgRoutes = SharedSvgRoutes;

  @Output() selectBoard = new EventEmitter<BoardResponse>();
  @Output() openInvite = new EventEmitter<void>();
  @Output() openSettings = new EventEmitter<void>();
  @Output() openCreate = new EventEmitter<void>();
  @Output() addBoard = new EventEmitter<void>();
}
