import { CommonModule } from '@angular/common';
import {
  Component,
  DestroyRef,
  Input,
  OnChanges,
  OnInit,
  SimpleChanges,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { AvatarComponent } from '../avatar/avatar.component';
import { CommentResponse } from '../../shared/interfaces';
import { CommentService } from '../../shared/services';

@Component({
  selector: 'app-task-comments',
  imports: [CommonModule, FormsModule, AvatarComponent],
  templateUrl: './task-comments.component.html',
  styleUrl: './task-comments.component.scss',
})
export class TaskCommentsComponent implements OnInit, OnChanges {
  @Input() public workItemId: number | null = null;
  @Input() public currentUserId = 0;
  @Input() public currentUserFirstName = 'User';
  @Input() public currentUserLastName = '';

  public readonly comments = signal<CommentResponse[]>([]);
  public readonly draft = signal('');
  public readonly editingCommentId = signal<number | null>(null);
  public readonly editDraft = signal('');
  public readonly isSubmitting = signal(false);
  public readonly isLoading = signal(false);
  public readonly errorMessage = signal('');

  private readonly commentService = inject(CommentService);
  private readonly destroyRef = inject(DestroyRef);

  public ngOnInit(): void {
    this.loadComments();
  }

  public ngOnChanges(changes: SimpleChanges): void {
    if (changes['workItemId'] && !changes['workItemId'].firstChange) {
      this.loadComments();
    }
  }

  public get pendingComments(): string[] {
    if (this.workItemId) {
      return [];
    }

    return this.comments().map((comment) => comment.content);
  }

  public authorName(comment: CommentResponse): string {
    return `${comment.firstName} ${comment.lastName}`.trim() || 'User';
  }

  public isOwnComment(comment: CommentResponse): boolean {
    return comment.userId === this.currentUserId;
  }

  public formatTimestamp(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    return date.toLocaleString(undefined, {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  public onDraftInput(value: string): void {
    this.draft.set(value);
  }

  public onEditDraftInput(value: string): void {
    this.editDraft.set(value);
  }

  public onSend(): void {
    const content = this.draft().trim();
    if (!content || this.isSubmitting()) {
      return;
    }

    if (!this.workItemId) {
      const localComment: CommentResponse = {
        id: -Date.now(),
        content,
        workItemId: 0,
        userId: this.currentUserId,
        firstName: this.currentUserFirstName,
        lastName: this.currentUserLastName,
        createdAt: new Date().toISOString(),
      };

      this.comments.update((items) => [...items, localComment]);
      this.draft.set('');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    this.commentService
      .create({ workItemId: this.workItemId, content })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.comments.update((items) => [...items, created]);
          this.draft.set('');
          this.isSubmitting.set(false);
        },
        error: (err) => {
          this.errorMessage.set(
            err.error?.message ?? 'Failed to add comment.'
          );
          this.isSubmitting.set(false);
        },
      });
  }

  public onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSend();
    }
  }

  public onStartEdit(comment: CommentResponse): void {
    this.editingCommentId.set(comment.id);
    this.editDraft.set(comment.content);
    this.errorMessage.set('');
  }

  public onCancelEdit(): void {
    this.editingCommentId.set(null);
    this.editDraft.set('');
  }

  public onSaveEdit(comment: CommentResponse): void {
    const content = this.editDraft().trim();
    if (!content || this.isSubmitting()) {
      return;
    }

    if (comment.id < 0) {
      this.comments.update((items) =>
        items.map((item) =>
          item.id === comment.id ? { ...item, content } : item
        )
      );
      this.onCancelEdit();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    this.commentService
      .update(comment.id, content)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.comments.update((items) =>
            items.map((item) => (item.id === updated.id ? updated : item))
          );
          this.onCancelEdit();
          this.isSubmitting.set(false);
        },
        error: (err) => {
          this.errorMessage.set(
            err.error?.message ?? 'Failed to update comment.'
          );
          this.isSubmitting.set(false);
        },
      });
  }

  public onDelete(comment: CommentResponse): void {
    if (this.isSubmitting()) {
      return;
    }

    if (comment.id < 0) {
      this.comments.update((items) =>
        items.filter((item) => item.id !== comment.id)
      );
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    this.commentService
      .delete(comment.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.comments.update((items) =>
            items.filter((item) => item.id !== comment.id)
          );
          this.isSubmitting.set(false);
        },
        error: (err) => {
          this.errorMessage.set(
            err.error?.message ?? 'Failed to delete comment.'
          );
          this.isSubmitting.set(false);
        },
      });
  }

  private loadComments(): void {
    if (!this.workItemId) {
      this.comments.set([]);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.commentService
      .getByWorkItemId(this.workItemId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.comments.set(items);
          this.isLoading.set(false);
        },
        error: (err) => {
          this.errorMessage.set(
            err.error?.message ?? 'Failed to load comments.'
          );
          this.isLoading.set(false);
        },
      });
  }
}
