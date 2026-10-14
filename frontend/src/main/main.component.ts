import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { SignalRService } from '../../shared/services';
import { ChatComponent } from '../chat/chat.component';
import { BoardWorkspaceStore } from './services/board-workspace.store';
import { TaskSessionService } from './services/task-session.service';
import { BoardRealtimeService } from './services/board-realtime.service';
import { WorkspaceSidebarComponent } from './components/workspace-sidebar/workspace-sidebar.component';
import { BoardHeaderComponent } from './components/board-header/board-header.component';
import { BacklogViewComponent } from './components/backlog-view/backlog-view.component';
import { KanbanBoardComponent } from './components/kanban-board/kanban-board.component';
import { BoardResponse } from '../../shared/interfaces';

type ToolbarTab = 'Backlog' | 'Board' | 'Chat';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss',
  providers: [BoardWorkspaceStore, TaskSessionService, BoardRealtimeService],
  imports: [
    CommonModule,
    ChatComponent,
    WorkspaceSidebarComponent,
    BoardHeaderComponent,
    BacklogViewComponent,
    KanbanBoardComponent,
  ],
})
export class MainComponent implements OnInit, OnDestroy {
  private static readonly TOOLBAR_TABS: ToolbarTab[] = [
    'Backlog',
    'Board',
    'Chat',
  ];

  readonly store = inject(BoardWorkspaceStore);
  readonly taskSession = inject(TaskSessionService);
  private readonly boardRealtime = inject(BoardRealtimeService);
  private readonly signalRService = inject(SignalRService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly selectedToolbarTab = signal<ToolbarTab>('Backlog');

  ngOnInit(): void {
    this.restoreContextFromUrl();
    this.store.loadCurrentUser();
    void this.signalRService.connect();
    this.boardRealtime.start();
    this.store.getWorkspace();
  }

  ngOnDestroy(): void {
    this.taskSession.destroySession();
    this.store.clearLockToastTimer();
    this.boardRealtime.clearBacklogRefreshTimer();
    void this.signalRService.leaveBoard();
    void this.signalRService.disconnect();
  }

  onToolbarTabSelect(tab: ToolbarTab): void {
    this.selectedToolbarTab.set(tab);
    this.syncTabToUrl(tab);
  }

  onSelectBoard(board: BoardResponse): void {
    this.store.onSelectBoard(board);
  }

  private restoreContextFromUrl(): void {
    const boardParam = this.route.snapshot.queryParamMap.get('board');
    if (boardParam) {
      const boardId = Number(boardParam);
      if (Number.isInteger(boardId) && boardId > 0) {
        this.store.selectedBoardId.set(boardId);
      }
    }

    const tabParam = this.route.snapshot.queryParamMap.get('tab');
    if (
      tabParam &&
      MainComponent.TOOLBAR_TABS.includes(tabParam as ToolbarTab)
    ) {
      this.selectedToolbarTab.set(tabParam as ToolbarTab);
    }
  }

  private syncTabToUrl(tab: ToolbarTab): void {
    const currentTab = this.router.parseUrl(this.router.url).queryParams['tab'];
    if (currentTab === tab) {
      return;
    }

    void this.router.navigate([], {
      queryParams: { tab },
      queryParamsHandling: 'merge',
    });
  }
}
