import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SvgIconComponent } from 'angular-svg-icon';
import { ChatBubbleComponent } from '../../components/chat-bubble/chat-bubble.component';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { ChatService, WorkspaceService } from '../../shared/services'; // Proveri tvoju putanju

interface DirectUser {
  id: number;
  firstName: string;
  lastName: string;
  conversationId?: number;
}

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
  public readonly currentUserId = Number(localStorage.getItem('user_id') ?? 0);

  private chatService = inject(ChatService);
  private readonly destroyRef = inject(DestroyRef);
  private workspaceService = inject(WorkspaceService);

  public readonly groups = [
    { id: 1, name: 'carrier' },
    { id: 2, name: 'release' },
  ];

  public readonly workspaceMembers = signal<DirectUser[]>([]);
  public readonly directUsers = signal<DirectUser[]>([]);
  public readonly searchTerm = signal('');
  public readonly isAddConversationOpen = signal(false);
  public selectedDirectUser: DirectUser | null = null;

  public ngOnInit(): void {
    this.loadWorkspaceMembers();
    this.loadExistingConversations();
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
                    (m: any) => ({
                      id: m.userId,
                      firstName: m.firstName ?? 'User',
                      lastName: m.lastName ?? '',
                    } as DirectUser)
                  );

                this.directUsers.update((existing) => {
                  const merged = new Map(existing.map((user) => [user.id, user]));
                  for (const user of workspaceUsers) {
                    const existingUser = merged.get(user.id);
                    if (existingUser) {
                      existingUser.firstName = user.firstName;
                      existingUser.lastName = user.lastName;
                    } else {
                      merged.set(user.id, user);
                    }
                  }
                  return Array.from(merged.values());
                });

                this.workspaceMembers.set(workspaceUsers);

                if (!this.selectedDirectUser && workspaceUsers.length > 0) {
                  this.selectedDirectUser = workspaceUsers[0];
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
        next: (conversations: any[]) => {
          const mappedUsers: DirectUser[] = conversations
            .flatMap((c: any) =>
              c.participants?.map((p: any) => ({
                id: p.userId,
                firstName: p.firstName ?? 'Unknown',
                lastName: p.lastName ?? 'User',
                conversationId: c.id,
              })) ?? []
            )
            .filter((u: any) => u.id !== this.currentUserId);

          this.directUsers.update((existing) => {
            const merged = new Map(existing.map((user) => [user.id, user]));
            for (const user of mappedUsers) {
              const existingUser = merged.get(user.id);
              if (existingUser) {
                existingUser.conversationId = user.conversationId;
                existingUser.firstName = user.firstName;
                existingUser.lastName = user.lastName;
              } else {
                merged.set(user.id, user);
              }
            }
            return Array.from(merged.values());
          });

          if (!this.selectedDirectUser && mappedUsers.length > 0) {
            this.selectedDirectUser = mappedUsers[0];
          }
        },
        error: (err) =>
          console.error('Greška pri učitavanju konverzacija:', err),
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

  public onAddConversationToggle(): void {
    this.isAddConversationOpen.update((value) => !value);
  }

  public onDirectUserSelect(userId: number): void {
    const selected = this.directUsers().find((user) => user.id === userId);
    if (!selected) {
      return;
    }
    this.selectedDirectUser = selected;
  }

  public onCreateConversation(user: DirectUser): void {
    if (!this.currentUserId || !user?.id) {
      return;
    }

    this.chatService.getConversationId(this.currentUserId, user.id).subscribe({
      next: (result) => {
        const conversation = {
          ...user,
          conversationId: result.conversationId,
        };

        this.selectedDirectUser = conversation;
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

  public getFilteredWorkspaceMembers(): DirectUser[] {
    const search = this.searchTerm().trim().toLowerCase();
    return this.workspaceMembers()
      .filter((member) => member.id !== this.currentUserId)
      .filter((member) =>
        `${member.firstName} ${member.lastName}`.toLowerCase().includes(search)
      );
  }
}
