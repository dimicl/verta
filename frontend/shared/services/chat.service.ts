import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface ChatApiMessage {
  id: number;
  content: string;
  senderId: number;
  conversationId: number;
  createdAt?: string;
}

export interface ConversationParticipant {
  id: number;
  userId: number;
  firstName: string;
  lastName: string;
  isOnline: boolean;
}

export interface ConversationResponse {
  id: number;
  type: string;
  name?: string;
  createdAt: string;
  unreadCount: number;
  participants: ConversationParticipant[];
}

interface SendMessageRequest {
  receiverId: number;
  content: string;
}

interface SendConversationMessageRequest {
  content: string;
}

interface CreateGroupConversationRequest {
  name: string;
  memberIds: number[];
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly apiUrl = 'http://localhost:8080/api/chat';

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
