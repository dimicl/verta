import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { InputComponent } from '../input/input.component';
import {
  AbstractControl,
  FormControl,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { BoardService, WorkspaceService } from '../../shared/services';
import { WorkspaceResponse } from '../../shared/interfaces/workspace-response.interface';
import { BoardRequest } from '../../shared/interfaces/board-request.interface';

@Component({
  selector: 'app-board-modal',
  imports: [CommonModule, InputComponent],
  templateUrl: './board-modal.component.html',
  styleUrl: './board-modal.component.scss',
})
export class BoardModalComponent {
  public workspaceMember: WorkspaceResponse | null = null;
  public boardNameValue = '';
  public createError = signal('');

  public workspaceNameValidator = (
    control: AbstractControl
  ): ValidationErrors | null => {
    const value = control.value;

    if (!value) return null;

    if (value.length < 5) {
      return { minLength: true };
    }

    if (/\d/.test(value)) {
      return { containsNumber: true };
    }

    if (value[0] !== value[0].toUpperCase()) {
      return { firstLetterUppercase: true };
    }

    return null;
  };

  public workspaceName = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, this.workspaceNameValidator],
  });

  constructor(
    private workspaceService: WorkspaceService,
    private boardService: BoardService,
    private activeModal: NgbActiveModal
  ) {
    this.getWorkspaceId();
  }

  private getWorkspaceId() {
    this.workspaceService.getWorkspace().subscribe({
      next: (result) => {
        this.workspaceMember = result;
      },
      error: (err) => {
        console.error(err);
      },
    });
  }

  public onBoardNameInput(value: string) {
    this.workspaceName.setValue(value);
    this.boardNameValue = value;
    this.createError.set('');
  }

  public onCreate() {
    if (this.workspaceName.invalid) {
      this.createError.set('Please enter a valid board name.');
      return;
    }

    const request: BoardRequest = {
      name: this.workspaceName.value,
      workspaceId: this.workspaceMember?.id ?? -1,
    };

    this.createError.set('');

    this.boardService.createBoard(request).subscribe({
      next: (result) => {
        this.activeModal.close(result);
      },
      error: (err) => {
        this.createError.set(
          err.error?.message ?? 'Board creation failed. Please try again.'
        );
      },
    });
  }

  public close(): void {
    this.activeModal.dismiss();
  }
}
