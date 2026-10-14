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

export interface SendMessageRequest {
  receiverId: number;
  content: string;
}

export interface SendConversationMessageRequest {
  content: string;
}

export interface CreateGroupConversationRequest {
  name: string;
  memberIds: number[];
}
