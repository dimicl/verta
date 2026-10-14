import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';
import {
  AppNotification,
  UserPresencePayload,
} from '../interfaces/notification.interface';
import { WorkItemWriteAccessPayload } from '../interfaces/work-item-lock-response.interface';
import { ChatApiMessage } from '../interfaces/chat.interfaces';
import { BoardRealtimeEvent } from '../interfaces/board-realtime-event.interface';
import { HUB_URL } from '../config/api.config';

export type { BoardRealtimeEvent } from '../interfaces/board-realtime-event.interface';

const BOARD_CONTENT_EVENTS = [
  'WorkItemCreated',
  'WorkItemUpdated',
  'WorkItemDeleted',
  'WorkItemPriorityChanged',
  'CommentCreated',
  'CommentUpdated',
  'CommentDeleted',
  'WorkItemFileAdded',
  'WorkItemFileDeleted',
  'BoardLocked',
  'BoardUnlocked',
  'BoardLockTransferred',
  'SubWorkItemCreated',
  'SubWorkItemUpdated',
  'SubWorkItemStatusChanged',
  'SubWorkItemDeleted',
  'SprintCreated',
  'SprintUpdated',
  'SprintDeleted',
  'WorkItemLocked',
  'WorkItemLockTransferred',
] as const;

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly hubUrl = HUB_URL;
  private hubConnection: signalR.HubConnection | null = null;
  private connectPromise: Promise<void> | null = null;
  private joinedBoardId: number | null = null;
  private joinBoardQueue: Promise<void> = Promise.resolve();

  private readonly notificationsSubject = new BehaviorSubject<AppNotification[]>(
    []
  );
  public readonly notifications$ = this.notificationsSubject.asObservable();

  private readonly chatMessageSubject = new Subject<ChatApiMessage>();
  public readonly chatMessage$ = this.chatMessageSubject.asObservable();

  private readonly userPresenceSubject = new Subject<UserPresencePayload>();
  public readonly userPresence$ = this.userPresenceSubject.asObservable();

  private readonly workItemWriteAccessSubject =
    new Subject<WorkItemWriteAccessPayload>();
  public readonly workItemWriteAccess$ =
    this.workItemWriteAccessSubject.asObservable();

  private readonly workItemUnlockedSubject = new Subject<number>();
  public readonly workItemUnlocked$ = this.workItemUnlockedSubject.asObservable();

  private readonly boardContentSubject = new Subject<BoardRealtimeEvent>();
  public readonly boardContent$ = this.boardContentSubject.asObservable();

  public async connect(): Promise<void> {
    const token = localStorage.getItem('token');
    if (!token) {
      return;
    }

    if (
      this.hubConnection?.state === signalR.HubConnectionState.Connected
    ) {
      return;
    }

    if (this.connectPromise) {
      return this.connectPromise;
    }

    this.connectPromise = this.startConnection();
    try {
      await this.connectPromise;
    } finally {
      this.connectPromise = null;
    }
  }

  private async startConnection(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => localStorage.getItem('token') ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ChatMessageReceived', (message: ChatApiMessage) => {
      this.chatMessageSubject.next(message);
    });

    this.hubConnection.on(
      'WorkItemAssigned',
      (payload: Record<string, unknown>) => {
        const name = String(payload['name'] ?? payload['Name'] ?? 'Task');
        this.pushNotification({
          eventName: 'WorkItemAssigned',
          title: 'Task assigned',
          message: `You were assigned to "${name}".`,
          payload,
        });
      }
    );

    this.hubConnection.on(
      'WorkItemStatusChanged',
      (payload: Record<string, unknown>) => {
        const name = String(payload['name'] ?? payload['Name'] ?? 'Task');
        const status = String(payload['status'] ?? payload['Status'] ?? '');
        this.pushNotification({
          eventName: 'WorkItemStatusChanged',
          title: 'Task status changed',
          message: `"${name}" is now ${status}.`,
          payload,
        });
      }
    );

    this.hubConnection.on(
      'WorkItemActivityNotification',
      (payload: Record<string, unknown>) => {
        const activity = this.formatActivityNotification(payload);
        if (!activity) {
          return;
        }
        this.pushNotification({
          eventName: 'WorkItemActivityNotification',
          title: activity.title,
          message: activity.message,
          payload,
        });
      }
    );

    this.hubConnection.on(
      'UserPresenceChanged',
      (payload: Record<string, unknown>) => {
        const userId = Number(payload['userId'] ?? payload['UserId'] ?? 0);
        const isOnline = Boolean(payload['isOnline'] ?? payload['IsOnline']);
        if (!userId) {
          return;
        }
        this.userPresenceSubject.next({ userId, isOnline });
      }
    );

    this.hubConnection.on(
      'YouNowHaveWriteAccess',
      (payload: Record<string, unknown>) => {
        const workItemId = Number(
          payload['workItemId'] ?? payload['WorkItemId'] ?? 0
        );
        if (!workItemId) {
          return;
        }

        const expiresAt = String(
          payload['expiresAt'] ?? payload['ExpiresAt'] ?? ''
        );

        this.workItemWriteAccessSubject.next({
          workItemId,
          expiresAt: expiresAt || undefined,
        });
      }
    );

    this.hubConnection.on(
      'WorkItemUnlocked',
      (payload: Record<string, unknown>) => {
        const workItemId = Number(
          payload['workItemId'] ?? payload['WorkItemId'] ?? 0
        );
        if (!workItemId) {
          return;
        }
        this.workItemUnlockedSubject.next(workItemId);
      }
    );

    for (const eventName of BOARD_CONTENT_EVENTS) {
      this.hubConnection.on(eventName, (payload: Record<string, unknown>) => {
        this.boardContentSubject.next({
          eventName,
          payload: payload ?? {},
        });
      });
    }

    this.hubConnection.onreconnected(async () => {
      if (this.joinedBoardId !== null) {
        await this.hubConnection?.invoke('JoinBoard', this.joinedBoardId);
      }
    });

    await this.hubConnection.start();

    if (this.joinedBoardId !== null) {
      await this.hubConnection.invoke('JoinBoard', this.joinedBoardId);
    }
  }

  public async joinBoard(boardId: number): Promise<void> {
    const run = async () => {
      if (boardId <= 0) {
        return;
      }

      await this.connect();

      if (!this.hubConnection) {
        return;
      }

      if (this.joinedBoardId === boardId) {
        return;
      }

      if (this.joinedBoardId !== null) {
        await this.leaveBoardInternal(this.joinedBoardId);
      }

      await this.hubConnection.invoke('JoinBoard', boardId);
      this.joinedBoardId = boardId;
    };

    this.joinBoardQueue = this.joinBoardQueue.then(run, run);
    await this.joinBoardQueue;
  }

  public async leaveBoard(boardId?: number): Promise<void> {
    const run = async () => {
      await this.leaveBoardInternal(boardId ?? this.joinedBoardId);
    };

    this.joinBoardQueue = this.joinBoardQueue.then(run, run);
    await this.joinBoardQueue;
  }

  private async leaveBoardInternal(boardId: number | null): Promise<void> {
    if (boardId === null || !this.hubConnection) {
      return;
    }

    try {
      if (this.hubConnection.state === signalR.HubConnectionState.Connected) {
        await this.hubConnection.invoke('LeaveBoard', boardId);
      }
    } finally {
      if (this.joinedBoardId === boardId) {
        this.joinedBoardId = null;
      }
    }
  }

  public async disconnect(): Promise<void> {
    this.joinedBoardId = null;
    this.joinBoardQueue = Promise.resolve();

    if (!this.hubConnection) {
      return;
    }

    await this.hubConnection.stop();
    this.hubConnection = null;
  }

  public get notifications(): AppNotification[] {
    return this.notificationsSubject.value;
  }

  public get unreadCount(): number {
    return this.notificationsSubject.value.filter((item) => !item.read).length;
  }

  public markAllAsRead(): void {
    const updated = this.notificationsSubject.value.map((item) => ({
      ...item,
      read: true,
    }));
    this.notificationsSubject.next(updated);
  }

  private pushNotification(input: {
    eventName: string;
    title: string;
    message: string;
    payload: Record<string, unknown>;
  }): void {
    const notification: AppNotification = {
      id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
      eventName: input.eventName,
      title: input.title,
      message: input.message,
      createdAt: new Date(),
      read: false,
      payload: input.payload,
    };

    this.notificationsSubject.next([
      notification,
      ...this.notificationsSubject.value,
    ]);
  }

  private formatActivityNotification(
    payload: Record<string, unknown>
  ): { title: string; message: string } | null {
    const activityType = String(
      payload['activityType'] ?? payload['ActivityType'] ?? ''
    );
    const name = String(payload['name'] ?? payload['Name'] ?? 'item');
    const fileName = String(payload['fileName'] ?? payload['FileName'] ?? 'file');
    const status = String(payload['status'] ?? payload['Status'] ?? '');

    switch (activityType) {
      case 'WorkItemUpdated':
        return {
          title: 'Task updated',
          message: `"${name}" was updated.`,
        };
      case 'SubWorkItemUpdated':
        return {
          title: 'Subtask updated',
          message: `"${name}" was updated.`,
        };
      case 'CommentCreated':
        return {
          title: 'New comment',
          message: `New comment on "${name}".`,
        };
      case 'WorkItemFileAdded':
        return {
          title: 'File added',
          message: `"${fileName}" was added to "${name}".`,
        };
      case 'WorkItemFileDeleted':
        return {
          title: 'File removed',
          message: `"${fileName}" was removed from "${name}".`,
        };
      case 'SubWorkItemStatusChanged':
        return {
          title: 'Subtask status changed',
          message: `"${name}" is now ${status}.`,
        };
      default:
        return null;
    }
  }
}
