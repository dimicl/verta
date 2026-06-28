import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

export interface ChatApiMessage {
  id: number;
  content: string;
  senderId: number;
  conversationId: number;
  createdAt?: string;
}

interface SendMessageRequest {
  receiverId: number;
  content: string;
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly apiUrl = 'http://localhost:8080/api/chat';
  private readonly hubUrl = 'http://localhost:8080/hubs/chat';
  private hubConnection: signalR.HubConnection | null = null;
  private connectedUserId: number | null = null;

  private readonly messageReceivedSubject = new Subject<ChatApiMessage>();
  public readonly messageReceived$: Observable<ChatApiMessage> =
    this.messageReceivedSubject.asObservable();

  constructor(private readonly httpClient: HttpClient) {}

  public async connect(userId: number): Promise<void> {
    if (!userId) {
      return;
    }

    if (this.hubConnection && this.connectedUserId === userId) {
      if (this.hubConnection.state === signalR.HubConnectionState.Connected) {
        return;
      }
      await this.hubConnection.start();
      await this.hubConnection.invoke('JoinUserGroup', userId);
      return;
    }

    await this.disconnect();

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => localStorage.getItem('token') ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ChatMessageReceived', (message: ChatApiMessage) => {
      this.messageReceivedSubject.next(message);
    });

    await this.hubConnection.start();
    await this.hubConnection.invoke('JoinUserGroup', userId);
    this.connectedUserId = userId;
  }

  public async disconnect(): Promise<void> {
    if (!this.hubConnection) {
      return;
    }

    if (
      this.connectedUserId &&
      this.hubConnection.state === signalR.HubConnectionState.Connected
    ) {
      await this.hubConnection.invoke('LeaveUserGroup', this.connectedUserId);
    }

    await this.hubConnection.stop();
    this.hubConnection = null;
    this.connectedUserId = null;
  }

  public sendMessage(payload: SendMessageRequest) {
    return this.httpClient.post<ChatApiMessage>(
      `${this.apiUrl}/messages`,
      payload
    );
  }

  public getMessages(conversationId: number) {
    return this.httpClient.get<ChatApiMessage[]>(
      `${this.apiUrl}/conversations/${conversationId}/messages`
    );
  }

  public getConversationId(
    senderId: number,
    receiverId: number
  ): Observable<{ conversationId: number }> {
    return this.httpClient.get<{ conversationId: number }>(
      `${this.apiUrl}/conversations/search`,
      { params: { senderId, receiverId } }
    );
  }

  public getMyConversations(): Observable<any[]> {
    return this.httpClient.get<any[]>(`${this.apiUrl}/conversations/my`);
  }
}
