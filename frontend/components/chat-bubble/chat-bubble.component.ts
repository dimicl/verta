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
import { ChatApiMessage, ChatService } from '../../shared/services';

type MessageSender = 'me' | 'them' | 'system';

interface ChatMessage {
  id: number;
  from: MessageSender;
  text: string;
  time: string;
  senderId: number;
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
  public readonly draft = signal('');
  public readonly messages = signal<ChatMessage[]>([]);
  private readonly destroyRef = inject(DestroyRef);
  private readonly chatService = inject(ChatService);
  private readonly knownMessageIds = new Set<number>();

  constructor() {
    effect(() => {
      const currentUserId = this.currentUserId();
      const receiverId = this.receiverId();

      // Reset ekrana pri promeni korisnika u sidebaru
      this.messages.set([]);
      this.knownMessageIds.clear();
      this.draft.set('');

      if (currentUserId && receiverId) {
        this.chatService
          .getConversationId(currentUserId, receiverId)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (res) => {
              const conversationId = res.conversationId;
              this.chatService
                .getMessages(conversationId)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({
                  next: (apiMessages) => {
                    const uiMessages = apiMessages.map((m) =>
                      this.mapToUiMessage(m)
                    );
                    this.messages.set(uiMessages);

                    apiMessages.forEach((m) => this.knownMessageIds.add(m.id));
                  },
                  error: (err) =>
                    console.error('Greška pri učitavanju istorije:', err),
                });
            },
            error: (err) =>
              console.error('Greška pri pronalaženju konverzacije:', err),
          });
      }
    });
  }

  public async ngOnInit(): Promise<void> {
    const userId = this.currentUserId();
    if (userId) {
      await this.chatService.connect(userId);
    }

    this.chatService.messageReceived$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((message) => {
        const currentUserId = this.currentUserId();
        const selectedReceiverId = this.receiverId();
        if (!currentUserId || !selectedReceiverId) {
          return;
        }

        const isCurrentConversation =
          message.senderId === selectedReceiverId ||
          message.senderId === currentUserId;

        if (!isCurrentConversation || this.knownMessageIds.has(message.id)) {
          return;
        }

        this.knownMessageIds.add(message.id);
        this.messages.update((items) => [
          ...items,
          this.mapToUiMessage(message),
        ]);
      });
  }

  public firstName(): string {
    return this.receiverFirstName();
  }

  public lastName(): string {
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
      console.log('co');

      event.preventDefault();
      const value = this.draft().trim();
      const senderId = this.currentUserId();
      const receiverId = this.receiverId();
      if (!value || !senderId || !receiverId) {
        return;
      }

      this.chatService
        .sendMessage({
          receiverId,
          content: value,
        })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (message) => {
            if (this.knownMessageIds.has(message.id)) {
              return;
            }

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

  private mapToUiMessage(message: ChatApiMessage): ChatMessage {
    const timestamp = message.createdAt
      ? new Date(message.createdAt)
      : new Date();
    const senderId = message.senderId;

    return {
      id: message.id,
      from: senderId === this.currentUserId() ? 'me' : 'them',
      text: message.content,
      time: this.toTimeLabel(timestamp),
      senderId,
      conversationId: message.conversationId,
      createdAt: timestamp,
    };
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
