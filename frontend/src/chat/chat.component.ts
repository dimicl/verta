import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SvgIconComponent } from 'angular-svg-icon';
import { ChatBubbleComponent } from '../../components/chat-bubble/chat-bubble.component';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import {
  ChatService,
  ConversationResponse,
  SignalRService,
  WorkspaceService,
} from '../../shared/services';

interface DirectUser {
  id: number;
  firstName: string;
  lastName: string;
  conversationId?: number;
  isOnline?: boolean;
  unreadCount?: number;
}

interface GroupConversation {
  id: number;
  name: string;
  unreadCount: number;
  participants: ConversationResponse['participants'];
}

type ChatSelection =
  | { type: 'direct'; user: DirectUser }
  | { type: 'group'; group: GroupConversation };

@Component({
  selector: 'app-chat',
  imports: [CommonModule, SvgIconComponent, ChatBubbleComponent],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss',
})
export class ChatComponent implements OnInit {
  public sharedSvgRoutes = SharedSvgRoutes;
  public isGroupsExpanded = true;
  public isDirectExpanded = true;
  public readonly currentUserId = ChatComponent.resolveCurrentUserId();

  private static resolveCurrentUserId(): number {
    const token = localStorage.getItem('token');
    if (token) {
      const parts = token.split('.');
      if (parts.length === 3) {
        try {
          const payload = JSON.parse(
            atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'))
          );
          const sub =
            payload.sub ??
            payload.nameid ??
            payload[
              'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
            ];
          const id = Number(sub);
          if (id) {
            return id;
          }
        } catch {
          // ignore malformed token, fall back to localStorage
        }
      }
    }

    return Number(localStorage.getItem('user_id') ?? 0);
  }

  private chatService = inject(ChatService);
  private signalRService = inject(SignalRService);
  private readonly destroyRef = inject(DestroyRef);
  private workspaceService = inject(WorkspaceService);

  public readonly groups = signal<GroupConversation[]>([]);
  public readonly workspaceMembers = signal<DirectUser[]>([]);
  public readonly directUsers = signal<DirectUser[]>([]);
  public readonly searchTerm = signal('');
  public readonly groupSearchTerm = signal('');
  public readonly newGroupName = signal('');
  public readonly selectedGroupMemberIds = signal<Set<number>>(new Set());
  public readonly isAddConversationOpen = signal(false);
  public readonly isAddGroupOpen = signal(false);
  public readonly selectedChat = signal<ChatSelection | null>(null);

  public readonly participants = computed<
    Record<number, { firstName: string; lastName: string }>
  >(() => {
    const selection = this.selectedChat();
    if (selection?.type !== 'group') {
      return {};
    }

    return selection.group.participants.reduce<
      Record<number, { firstName: string; lastName: string }>
    >((acc, participant) => {
      acc[participant.userId] = {
        firstName: participant.firstName,
        lastName: participant.lastName,
      };
      return acc;
    }, {});
  });

  public readonly selectedDirectUser = computed(() => {
    const selection = this.selectedChat();
    return selection?.type === 'direct' ? selection.user : null;
  });

  public readonly selectedGroup = computed(() => {
    const selection = this.selectedChat();
    return selection?.type === 'group' ? selection.group : null;
  });

  public async ngOnInit(): Promise<void> {
    await this.signalRService.connect();
    this.loadWorkspaceMembers();
    this.loadExistingConversations();
    this.listenForRealtimeUpdates();
  }

  private listenForRealtimeUpdates(): void {
    this.signalRService.userPresence$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ userId, isOnline }) => {
        this.updateUserPresence(userId, isOnline);
      });

    this.signalRService.chatMessage$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((message) => {
        if (message.senderId === this.currentUserId) {
          return;
        }

        this.directUsers.update((users) =>
          users.map((user) => {
            const selection = this.selectedChat();
            if (
              user.conversationId === message.conversationId &&
              user.id === message.senderId &&
              selection?.type !== 'direct'
            ) {
              return {
                ...user,
                unreadCount: (user.unreadCount ?? 0) + 1,
              };
            }
            if (
              user.conversationId === message.conversationId &&
              user.id === message.senderId &&
              selection?.type === 'direct' &&
              selection.user.id !== user.id
            ) {
              return {
                ...user,
                unreadCount: (user.unreadCount ?? 0) + 1,
              };
            }
            return user;
          })
        );

        this.groups.update((groupList) =>
          groupList.map((group) => {
            const selection = this.selectedChat();
            if (
              group.id === message.conversationId &&
              (selection?.type !== 'group' || selection.group.id !== group.id)
            ) {
              return {
                ...group,
                unreadCount: group.unreadCount + 1,
              };
            }
            return group;
          })
        );
      });
  }

  private updateUserPresence(userId: number, isOnline: boolean): void {
    const applyPresence = (users: DirectUser[]) =>
      users.map((user) =>
        user.id === userId ? { ...user, isOnline } : user
      );

    this.directUsers.update(applyPresence);
    this.workspaceMembers.update(applyPresence);
  }

  private loadWorkspaceMembers(): void {
    this.workspaceService
      .getWorkspace()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (workspace) => {
          if (!workspace?.id) {
            return;
          }

          this.workspaceService
            .getMembers(workspace.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: (members: any[]) => {
                const workspaceUsers = members
                  .filter((m) => m.userId !== this.currentUserId)
                  .map(
                    (m: any) =>
                      ({
                        id: m.userId,
                        firstName: m.firstName ?? 'User',
                        lastName: m.lastName ?? '',
                        isOnline: m.isOnline ?? false,
                        unreadCount: 0,
                      }) as DirectUser
                  );

                this.directUsers.update((existing) => {
                  const merged = new Map(existing.map((user) => [user.id, user]));
                  for (const user of workspaceUsers) {
                    const existingUser = merged.get(user.id);
                    if (existingUser) {
                      existingUser.firstName = user.firstName;
                      existingUser.lastName = user.lastName;
                      existingUser.isOnline = user.isOnline;
                    } else {
                      merged.set(user.id, user);
                    }
                  }
                  return Array.from(merged.values());
                });

                this.workspaceMembers.set(workspaceUsers);

                if (!this.selectedChat() && workspaceUsers.length > 0) {
                  this.selectedChat.set({ type: 'direct', user: workspaceUsers[0] });
                }
              },
              error: (err) =>
                console.error('Greška pri učitavanju članova radnog prostora:', err),
            });
        },
        error: (err) =>
          console.error('Greška pri učitavanju radnog prostora:', err),
      });
  }

  private loadExistingConversations(): void {
    this.chatService
      .getMyConversations()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (conversations) => {
          for (const conversation of conversations) {
            if (conversation.type === 'Group') {
              this.upsertGroup({
                id: conversation.id,
                name: conversation.name ?? 'Group',
                unreadCount: conversation.unreadCount ?? 0,
                participants: conversation.participants ?? [],
              });
              continue;
            }

            const otherParticipant = conversation.participants?.find(
              (p) => p.userId !== this.currentUserId
            );

            if (!otherParticipant) {
              continue;
            }

            const mappedUser: DirectUser = {
              id: otherParticipant.userId,
              firstName: otherParticipant.firstName ?? 'Unknown',
              lastName: otherParticipant.lastName ?? 'User',
              conversationId: conversation.id,
              isOnline: otherParticipant.isOnline ?? false,
              unreadCount: conversation.unreadCount ?? 0,
            };

            this.directUsers.update((existing) => {
              const merged = new Map(existing.map((user) => [user.id, user]));
              const existingUser = merged.get(mappedUser.id);
              if (existingUser) {
                existingUser.conversationId = mappedUser.conversationId;
                existingUser.firstName = mappedUser.firstName;
                existingUser.lastName = mappedUser.lastName;
                existingUser.isOnline = mappedUser.isOnline;
                existingUser.unreadCount = mappedUser.unreadCount;
              } else {
                merged.set(mappedUser.id, mappedUser);
              }
              return Array.from(merged.values());
            });
          }

          if (!this.selectedChat() && this.directUsers().length > 0) {
            this.selectedChat.set({
              type: 'direct',
              user: this.directUsers()[0],
            });
          }
        },
        error: (err) =>
          console.error('Greška pri učitavanju konverzacija:', err),
      });
  }

  private upsertGroup(group: GroupConversation): void {
    this.groups.update((existing) => {
      const merged = new Map(existing.map((item) => [item.id, item]));
      const current = merged.get(group.id);
      if (current) {
        merged.set(group.id, { ...current, ...group });
      } else {
        merged.set(group.id, group);
      }
      return Array.from(merged.values());
    });
  }

  public onHeaderGroupsToggle(): void {
    this.isGroupsExpanded = !this.isGroupsExpanded;
  }

  public onHeaderDirectToggle(): void {
    this.isDirectExpanded = !this.isDirectExpanded;
  }

  public onSearchTermChange(value: string): void {
    this.searchTerm.set(value);
  }

  public onGroupSearchTermChange(value: string): void {
    this.groupSearchTerm.set(value);
  }

  public onNewGroupNameChange(value: string): void {
    this.newGroupName.set(value);
  }

  public onAddConversationToggle(event: Event): void {
    event.stopPropagation();
    this.isAddConversationOpen.update((value) => !value);
    this.isAddGroupOpen.set(false);
  }

  public onAddGroupToggle(event: Event): void {
    event.stopPropagation();
    this.isAddGroupOpen.update((value) => !value);
    this.isAddConversationOpen.set(false);
    this.newGroupName.set('');
    this.selectedGroupMemberIds.set(new Set());
    this.groupSearchTerm.set('');
  }

  public onDirectUserSelect(userId: number): void {
    const selected = this.directUsers().find((user) => user.id === userId);
    if (!selected) {
      return;
    }

    this.selectedChat.set({ type: 'direct', user: selected });

    if (selected.conversationId) {
      this.chatService.markAsRead(selected.conversationId).subscribe({
        next: () => {
          this.directUsers.update((users) =>
            users.map((user) =>
              user.id === userId ? { ...user, unreadCount: 0 } : user
            )
          );
        },
      });
    }
  }

  public onGroupSelect(groupId: number): void {
    const selected = this.groups().find((group) => group.id === groupId);
    if (!selected) {
      return;
    }

    this.selectedChat.set({ type: 'group', group: selected });

    this.chatService.markAsRead(groupId).subscribe({
      next: () => {
        this.groups.update((groupList) =>
          groupList.map((group) =>
            group.id === groupId ? { ...group, unreadCount: 0 } : group
          )
        );
      },
    });
  }

  public onCreateConversation(user: DirectUser): void {
    if (!this.currentUserId || !user?.id) {
      return;
    }

    this.chatService.getConversationId(user.id).subscribe({
      next: (result) => {
        const conversation = {
          ...user,
          conversationId: result.conversationId,
          unreadCount: 0,
        };

        this.selectedChat.set({ type: 'direct', user: conversation });
        this.directUsers.update((existing) => {
          const merged = new Map(existing.map((item) => [item.id, item]));
          merged.set(user.id, conversation);
          return Array.from(merged.values());
        });

        this.isAddConversationOpen.set(false);
      },
      error: (err) =>
        console.error('Greška pri kreiranju konverzacije:', err),
    });
  }

  public onToggleGroupMember(memberId: number): void {
    this.selectedGroupMemberIds.update((ids) => {
      const next = new Set(ids);
      if (next.has(memberId)) {
        next.delete(memberId);
      } else {
        next.add(memberId);
      }
      return next;
    });
  }

  public onCreateGroup(): void {
    const name = this.newGroupName().trim();
    const memberIds = Array.from(this.selectedGroupMemberIds());

    if (!name || memberIds.length === 0) {
      return;
    }

    this.chatService.createGroupConversation(name, memberIds).subscribe({
      next: (conversation) => {
        const group: GroupConversation = {
          id: conversation.id,
          name: conversation.name ?? name,
          unreadCount: 0,
          participants: conversation.participants ?? [],
        };

        this.upsertGroup(group);
        this.selectedChat.set({ type: 'group', group });
        this.isAddGroupOpen.set(false);
        this.newGroupName.set('');
        this.selectedGroupMemberIds.set(new Set());
        this.groupSearchTerm.set('');
      },
      error: (err) =>
        console.error('Greška pri kreiranju grupe:', err),
    });
  }

  public getFilteredWorkspaceMembers(): DirectUser[] {
    const search = this.searchTerm().trim().toLowerCase();
    return this.workspaceMembers()
      .filter((member) => member.id !== this.currentUserId)
      .filter((member) =>
        `${member.firstName} ${member.lastName}`.toLowerCase().includes(search)
      );
  }

  public getFilteredGroupMembers(): DirectUser[] {
    const search = this.groupSearchTerm().trim().toLowerCase();
    if (!search) {
      return [];
    }

    return this.workspaceMembers()
      .filter((member) => member.id !== this.currentUserId)
      .filter((member) =>
        `${member.firstName} ${member.lastName}`.toLowerCase().includes(search)
      );
  }

  public isGroupMemberSelected(memberId: number): boolean {
    return this.selectedGroupMemberIds().has(memberId);
  }

  public getSelectedGroupMembers(): DirectUser[] {
    const ids = this.selectedGroupMemberIds();
    return this.workspaceMembers().filter((member) => ids.has(member.id));
  }

  public isDirectSelected(userId: number): boolean {
    const selection = this.selectedChat();
    return selection?.type === 'direct' && selection.user.id === userId;
  }

  public isGroupChatSelected(groupId: number): boolean {
    const selection = this.selectedChat();
    return selection?.type === 'group' && selection.group.id === groupId;
  }
}
