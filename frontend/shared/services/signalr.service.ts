import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import {
  AppNotification,
  UserPresencePayload,
} from '../interfaces/notification.interface';
import { WorkItemWriteAccessPayload } from '../interfaces/work-item-lock-response.interface';
import { ChatApiMessage } from '../services/chat.service';

export interface BoardRealtimeEvent {
  eventName: string;
  payload: Record<string, unknown>;
}

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
] as const;

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly hubUrl = 'http://localhost:8080/hubs/chat';
  private hubConnection: signalR.HubConnection | null = null;
  private connectPromise: Promise<void> | null = null;
  private joinedBoardId: number | null = null;

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

    this.connectPromise = this.startConnection(token);
    try {
      await this.connectPromise;
    } finally {
      this.connectPromise = null;
    }
  }

  private async startConnection(token: string): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => token,
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
      await this.leaveBoard(this.joinedBoardId);
    }

    await this.hubConnection.invoke('JoinBoard', boardId);
    this.joinedBoardId = boardId;
  }

  public async leaveBoard(boardId?: number): Promise<void> {
    const target = boardId ?? this.joinedBoardId;
    if (target === null || !this.hubConnection) {
      return;
    }

    try {
      if (this.hubConnection.state === signalR.HubConnectionState.Connected) {
        await this.hubConnection.invoke('LeaveBoard', target);
      }
    } finally {
      if (this.joinedBoardId === target) {
        this.joinedBoardId = null;
      }
    }
  }

  public async disconnect(): Promise<void> {
    if (!this.hubConnection) {
      return;
    }

    this.joinedBoardId = null;
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
}
