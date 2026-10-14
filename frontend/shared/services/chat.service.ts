import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../config/api.config';
import {
  ChatApiMessage,
  ConversationResponse,
  CreateGroupConversationRequest,
  SendConversationMessageRequest,
  SendMessageRequest,
} from '../interfaces/chat.interfaces';

export type {
  ChatApiMessage,
  ConversationParticipant,
  ConversationResponse,
} from '../interfaces/chat.interfaces';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly apiUrl = `${API_URL}/chat`;

  constructor(private readonly httpClient: HttpClient) {}

  public sendMessage(payload: SendMessageRequest) {
    return this.httpClient.post<ChatApiMessage>(
      `${this.apiUrl}/messages`,
      payload
    );
  }

  public sendMessageToConversation(conversationId: number, content: string) {
    return this.httpClient.post<ChatApiMessage>(
      `${this.apiUrl}/conversations/${conversationId}/messages`,
      { content } satisfies SendConversationMessageRequest
    );
  }

  public getMessages(conversationId: number) {
    return this.httpClient.get<ChatApiMessage[]>(
      `${this.apiUrl}/conversations/${conversationId}/messages`
    );
  }

  public markAsRead(conversationId: number) {
    return this.httpClient.post<void>(
      `${this.apiUrl}/conversations/${conversationId}/read`,
      null
    );
  }

  public getUnreadCount(conversationId: number): Observable<{ unreadCount: number }> {
    return this.httpClient.get<{ conversationId: number; unreadCount: number }>(
      `${this.apiUrl}/conversations/${conversationId}/unread`
    );
  }

  public getConversationId(receiverId: number): Observable<{ conversationId: number }> {
    return this.httpClient.get<{ conversationId: number }>(
      `${this.apiUrl}/conversations/search`,
      { params: { receiverId } }
    );
  }

  public getMyConversations(): Observable<ConversationResponse[]> {
    return this.httpClient.get<ConversationResponse[]>(`${this.apiUrl}/conversations/my`);
  }

  public createGroupConversation(name: string, memberIds: number[]) {
    return this.httpClient.post<ConversationResponse>(
      `${this.apiUrl}/conversations/group`,
      { name, memberIds } satisfies CreateGroupConversationRequest
    );
  }
}
