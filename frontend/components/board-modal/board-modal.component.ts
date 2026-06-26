import { Component, EventEmitter, Output } from '@angular/core';
import { InputComponent } from '../input/input.component';
import {
  AbstractControl,
  FormControl,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { BoardService, WorkspaceService } from '../../shared/services';
import { WorkspaceResponse } from '../../shared/interfaces/workspace-response.interface';
import { BoardRequest } from '../../shared/interfaces/board-request.interface';

@Component({
  selector: 'app-board-modal',
  imports: [InputComponent],
  templateUrl: './board-modal.component.html',
  styleUrl: './board-modal.component.scss',
})
export class BoardModalComponent {
  public workspaceMember: WorkspaceResponse | null = null;

  @Output() onEmitOwnerId = new EventEmitter<void>();
  @Output() closeModal = new EventEmitter<void>();

  constructor(
    private workspaceService: WorkspaceService,
    private boardService: BoardService
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

  public onInput(event: string) {
    this.workspaceName.setValue(event);
  }

  public onCreate() {
    const request: BoardRequest = {
      name: this.workspaceName.value,
      workspaceId: this.workspaceMember?.id ?? -1,
    };

    this.boardService.createBoard(request).subscribe((result) => {});
  }

  public close(): void {
    this.closeModal.emit();
  }
}
