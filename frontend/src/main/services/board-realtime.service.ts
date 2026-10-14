import { DestroyRef, Injectable, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SignalRService, TaskService } from '../../../shared/services';
import { BoardRealtimeEvent } from '../../../shared/interfaces/board-realtime-event.interface';
import { TaskModalComponent } from '../../../components/task-modal/task-modal.component';
import { TaskStatus } from '../../../shared/types/task-status.type';
import { BoardWorkspaceStore } from './board-workspace.store';
import { TaskSessionService } from './task-session.service';

@Injectable()
export class BoardRealtimeService {
  private readonly signalRService = inject(SignalRService);
  private readonly store = inject(BoardWorkspaceStore);
  private readonly taskSession = inject(TaskSessionService);
  private readonly taskService = inject(TaskService);
  private readonly destroyRef = inject(DestroyRef);

  private backlogRefreshTimer: ReturnType<typeof setTimeout> | null = null;

  start(): void {
    this.signalRService.boardContent$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event) => this.onBoardContentEvent(event));

    this.signalRService.workItemWriteAccess$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((payload) => {
        if (this.taskSession.activeWorkItemId !== payload.workItemId) {
          return;
        }

        const modal = this.taskSession.openTaskModalRef?.componentInstance as
          | TaskModalComponent
          | undefined;
        if (modal?.isReadOnly) {
          modal.grantWriteAccess();
          this.taskSession.startHeartbeat(payload.workItemId);
          this.store.stickyLockMessage = 'You now have write access.';
        }
      });

    this.signalRService.workItemUnlocked$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((workItemId) => {
        if (this.taskSession.activeWorkItemId === workItemId) {
          return;
        }

        this.store.stickyLockMessage = `Task #${workItemId} is now available for editing.`;
      });
  }

  clearBacklogRefreshTimer(): void {
    if (this.backlogRefreshTimer) {
      clearTimeout(this.backlogRefreshTimer);
      this.backlogRefreshTimer = null;
    }
  }

  private onBoardContentEvent(event: BoardRealtimeEvent): void {
    const boardId = Number(
      event.payload['boardId'] ?? event.payload['BoardId'] ?? 0
    );
    const selectedBoardId = this.store.selectedBoardId();
    if (!boardId || selectedBoardId !== boardId) {
      return;
    }

    const workItemId = Number(
      event.payload['workItemId'] ?? event.payload['WorkItemId'] ?? 0
    );

    this.scheduleBacklogRefresh();

    if (!workItemId || this.taskSession.activeWorkItemId !== workItemId) {
      return;
    }

    const modal = this.taskSession.openTaskModalRef?.componentInstance as
      | TaskModalComponent
      | undefined;
    if (!modal) {
      return;
    }

    const contentEvents = new Set([
      'CommentCreated',
      'CommentUpdated',
      'CommentDeleted',
      'SubWorkItemCreated',
      'SubWorkItemUpdated',
      'SubWorkItemStatusChanged',
      'SubWorkItemDeleted',
      'WorkItemFileAdded',
      'WorkItemFileDeleted',
    ]);

    if (contentEvents.has(event.eventName)) {
      modal.reloadLiveContent();
    }

    if (
      event.eventName === 'WorkItemUpdated' ||
      event.eventName === 'WorkItemPriorityChanged'
    ) {
      if (!modal.isReadOnly) {
        return;
      }

      const title = this.readPayloadString(event.payload, 'name', 'Name');
      const description = this.readPayloadString(
        event.payload,
        'description',
        'Description'
      );
      const status = this.readPayloadString(
        event.payload,
        'status',
        'Status'
      ) as TaskStatus | '';
      const hasAssignee =
        'assignedUserId' in event.payload || 'AssignedUserId' in event.payload;
      const assignedRaw =
        event.payload['assignedUserId'] ?? event.payload['AssignedUserId'];
      const hasSprint =
        'sprintId' in event.payload || 'SprintId' in event.payload;
      const sprintRaw = event.payload['sprintId'] ?? event.payload['SprintId'];

      if (title || description || status || hasAssignee || hasSprint) {
        modal.applyRemoteFields({
          title: title || undefined,
          description: description || undefined,
          status: status || undefined,
          assignedUserId: hasAssignee
            ? assignedRaw == null
              ? null
              : Number(assignedRaw)
            : undefined,
          sprintId: hasSprint
            ? sprintRaw == null
              ? null
              : Number(sprintRaw)
            : undefined,
        });
        return;
      }

      this.taskService.getById(workItemId).subscribe({
        next: (task) => {
          modal.applyRemoteFields({
            title: task.name,
            description: task.description,
            status: task.status,
            assignedUserId: task.assignedUserId ?? null,
            sprintId: task.sprintId ?? null,
          });
        },
        error: (err) =>
          console.error('Greška pri sinhronizaciji taska uživo:', err),
      });
    }
  }

  private readPayloadString(
    payload: Record<string, unknown>,
    camel: string,
    pascal: string
  ): string {
    const value = payload[camel] ?? payload[pascal];
    return value == null ? '' : String(value);
  }

  private scheduleBacklogRefresh(): void {
    if (this.backlogRefreshTimer) {
      clearTimeout(this.backlogRefreshTimer);
    }

    this.backlogRefreshTimer = setTimeout(() => {
      this.backlogRefreshTimer = null;
      this.store.refreshBacklog();
    }, 300);
  }
}
