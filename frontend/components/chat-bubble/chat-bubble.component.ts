import { CommonModule } from '@angular/common';
import {
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AvatarComponent } from '../avatar/avatar.component';
import {
  ChatApiMessage,
  ChatService,
  SignalRService,
} from '../../shared/services';

type MessageSender = 'me' | 'them' | 'system';

export interface ChatParticipant {
  firstName: string;
  lastName: string;
}

interface ChatMessage {
  id: number;
  from: MessageSender;
  text: string;
  time: string;
  senderId: number;
  senderFirstName: string;
  senderLastName: string;
  conversationId: number;
  createdAt: Date;
}

interface MessageGroup {
  dateKey: string;
  dateLabel: string;
  messages: ChatMessage[];
}

@Component({
  selector: 'app-chat-bubble',
  imports: [CommonModule, AvatarComponent],
  templateUrl: './chat-bubble.component.html',
  styleUrl: './chat-bubble.component.scss',
})
export class ChatBubbleComponent implements OnInit {
  public readonly currentUserId = input<number>(0);
  public readonly receiverId = input<number>(0);
  public readonly receiverFirstName = input<string>('User');
  public readonly receiverLastName = input<string>('');
  public readonly conversationId = input<number | null>(null);
  public readonly isGroup = input<boolean>(false);
  public readonly groupName = input<string>('');
  public readonly participants = input<Record<number, ChatParticipant>>({});
  public readonly draft = signal('');
  public readonly messages = signal<ChatMessage[]>([]);
  private readonly destroyRef = inject(DestroyRef);
  private readonly chatService = inject(ChatService);
  private readonly signalRService = inject(SignalRService);
  private readonly knownMessageIds = new Set<number>();
  private activeConversationId: number | null = null;

  constructor() {
    effect(() => {
      const currentUserId = this.currentUserId();
      const receiverId = this.receiverId();
      const conversationId = this.conversationId();
      const isGroup = this.isGroup();

      this.messages.set([]);
      this.knownMessageIds.clear();
      this.draft.set('');
      this.activeConversationId = null;

      if (isGroup && conversationId) {
        this.loadConversation(conversationId);
        return;
      }

      if (currentUserId && receiverId) {
        this.chatService
          .getConversationId(receiverId)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (res) => this.loadConversation(res.conversationId),
            error: (err) =>
              console.error('Greška pri pronalaženju konverzacije:', err),
          });
      }
    });
  }

  public async ngOnInit(): Promise<void> {
    await this.signalRService.connect();

    this.signalRService.chatMessage$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((message) => {
        const currentUserId = this.currentUserId();
        const selectedReceiverId = this.receiverId();
        const isGroup = this.isGroup();

        if (!currentUserId) {
          return;
        }

        const isCurrentConversation =
          message.conversationId === this.activeConversationId ||
          (!isGroup &&
            (message.senderId === selectedReceiverId ||
              (message.senderId === currentUserId &&
                message.conversationId === this.activeConversationId)));

        if (!isCurrentConversation || this.knownMessageIds.has(message.id)) {
          return;
        }

        this.knownMessageIds.add(message.id);
        this.messages.update((items) => [
          ...items,
          this.mapToUiMessage(message),
        ]);

        if (
          this.activeConversationId &&
          message.senderId !== currentUserId
        ) {
          this.chatService.markAsRead(this.activeConversationId).subscribe();
        }
      });
  }

  public displayFirstName(): string {
    if (this.isGroup()) {
      return this.groupName() || 'Group';
    }
    return this.receiverFirstName();
  }

  public displayLastName(): string {
    if (this.isGroup()) {
      return '';
    }
    return this.receiverLastName();
  }

  public messageGroups(): MessageGroup[] {
    const grouped = new Map<string, ChatMessage[]>();

    for (const message of this.messages()) {
      const dateKey = this.toDateKey(message);
      if (!grouped.has(dateKey)) {
        grouped.set(dateKey, []);
      }

      grouped.get(dateKey)?.push(message);
    }

    return Array.from(grouped.entries()).map(([dateKey, messages]) => ({
      dateKey,
      dateLabel: this.toDateLabel(dateKey),
      messages,
    }));
  }

  public onDraftInput(value: string): void {
    this.draft.set(value);
  }

  public onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      const value = this.draft().trim();
      const senderId = this.currentUserId();
      if (!value || !senderId) {
        return;
      }

      const send$ = this.isGroup() && this.activeConversationId
        ? this.chatService.sendMessageToConversation(
            this.activeConversationId,
            value
          )
        : this.receiverId()
          ? this.chatService.sendMessage({
              receiverId: this.receiverId(),
              content: value,
            })
          : null;

      if (!send$) {
        return;
      }

      send$
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (message) => {
            if (this.knownMessageIds.has(message.id)) {
              return;
            }

            this.activeConversationId = message.conversationId;
            this.knownMessageIds.add(message.id);
            this.messages.update((items) => [
              ...items,
              this.mapToUiMessage(message),
            ]);
          },
          error: (err) =>
            console.error('Greška pri slanju poruke:', err),
        });

      this.draft.set('');
    }
  }

  private loadConversation(conversationId: number): void {
    this.activeConversationId = conversationId;
    this.chatService.markAsRead(conversationId).subscribe();

    this.chatService
      .getMessages(conversationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (apiMessages) => {
          const uiMessages = apiMessages.map((m) => this.mapToUiMessage(m));
          this.messages.set(uiMessages);
          apiMessages.forEach((m) => this.knownMessageIds.add(m.id));
        },
        error: (err) =>
          console.error('Greška pri učitavanju istorije:', err),
      });
  }

  private mapToUiMessage(message: ChatApiMessage): ChatMessage {
    const timestamp = message.createdAt
      ? new Date(message.createdAt)
      : new Date();
    const senderId = message.senderId;
    const isMe = senderId === this.currentUserId();
    const participant = this.participants()[senderId];

    let firstName = participant?.firstName ?? '';
    let lastName = participant?.lastName ?? '';

    if (!firstName && !isMe) {
      firstName = this.receiverFirstName() ?? 'User';
      lastName = this.receiverLastName() ?? '';
    }

    if (!firstName && isMe) {
      firstName = 'You';
    }

    return {
      id: message.id,
      from: isMe ? 'me' : 'them',
      text: message.content,
      time: this.toTimeLabel(timestamp),
      senderId,
      senderFirstName: firstName,
      senderLastName: lastName,
      conversationId: message.conversationId,
      createdAt: timestamp,
    };
  }

  public senderLabel(message: ChatMessage): string {
    return `${message.senderFirstName} ${message.senderLastName}`.trim();
  }

  private toTimeLabel(date: Date): string {
    return `${date.getHours().toString().padStart(2, '0')}:${date
      .getMinutes()
      .toString()
      .padStart(2, '0')}`;
  }

  private toDateKey(message: ChatMessage): string {
    const date = message.createdAt;
    return `${date.getFullYear()}-${(date.getMonth() + 1)
      .toString()
      .padStart(2, '0')}-${date.getDate().toString().padStart(2, '0')}`;
  }

  private toDateLabel(dateKey: string): string {
    const today = new Date();
    const yesterday = new Date();
    yesterday.setDate(today.getDate() - 1);
    const todayKey = `${today.getFullYear()}-${(today.getMonth() + 1)
      .toString()
      .padStart(2, '0')}-${today.getDate().toString().padStart(2, '0')}`;
    const yesterdayKey = `${yesterday.getFullYear()}-${(
      yesterday.getMonth() + 1
    )
      .toString()
      .padStart(2, '0')}-${yesterday.getDate().toString().padStart(2, '0')}`;

    if (dateKey === todayKey) {
      return 'Today';
    }

    if (dateKey === yesterdayKey) {
      return 'Yesterday';
    }

    return dateKey;
  }
}
