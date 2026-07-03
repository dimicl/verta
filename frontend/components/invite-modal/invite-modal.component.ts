import { CommonModule } from '@angular/common';
import { Component, HostListener, signal } from '@angular/core';
import { InputComponent } from '../input/input.component';
import { FormControl, Validators } from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { BoardService, WorkspaceService } from '../../shared/services';

@Component({
  selector: 'app-invite-modal',
  imports: [CommonModule, InputComponent],
  templateUrl: './invite-modal.component.html',
  styleUrl: './invite-modal.component.scss',
})
export class InviteModalComponent {
  public invitedEmail = new FormControl('', [
    Validators.required,
    Validators.email,
  ]);
  public workspaceId: number | null = null;
  public boardId: number | null = null;
  public boardName: string | null = null;
  public emailValue = '';
  public inviteError = signal('');

  constructor(
    private workspaceService: WorkspaceService,
    private boardService: BoardService,
    private activeModal: NgbActiveModal
  ) {}

  @HostListener('document:keydown', ['$event'])
  public onDocumentKeyDown(event: KeyboardEvent): void {
    if (event.key !== 'Backspace') {
      return;
    }

    const target = event.target as HTMLElement | null;
    const tagName = target?.tagName?.toLowerCase();
    const isEditable =
      tagName === 'input' ||
      tagName === 'textarea' ||
      target?.isContentEditable;

    if (!isEditable) {
      event.preventDefault();
    }
  }

  public onInput(value: string): void {
    this.invitedEmail.setValue(value);
    this.emailValue = value;
    this.inviteError.set('');
  }

  public onInviteUser(): void {
    if (this.invitedEmail.invalid) {
      this.inviteError.set('Please enter a valid email address.');
      return;
    }

    this.inviteError.set('');

    if (this.boardId) {
      this.boardService
        .inviteToBoard({
          boardId: this.boardId,
          email: this.invitedEmail.value ?? '',
        })
        .subscribe({
          next: (result) => {
            this.activeModal.close(result);
          },
          error: (err) => {
            this.invitedEmail.reset();
            this.emailValue = '';
            this.inviteError.set(
              err.error?.message ?? 'Invite failed. Please try again.'
            );
          },
        });
      return;
    }

    const request = {
      WorkspaceId: this.workspaceId,
      Email: this.invitedEmail.value,
    };

    this.workspaceService.inviteUser(request).subscribe({
      next: (result) => {
        this.activeModal.close(result);
      },
      error: (err) => {
        this.invitedEmail.reset();
        this.emailValue = '';
        this.inviteError.set(
          err.error?.message ?? 'Invite failed. Please try again.'
        );
      },
    });
  }
}
