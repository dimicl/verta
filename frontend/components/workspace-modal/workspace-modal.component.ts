import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { InputComponent } from '../input/input.component';
import {
  AbstractControl,
  FormControl,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { WorkspaceService } from '../../shared/services';
import {
  WorkspaceMemberResponse,
  WorkspaceRequest,
} from '../../shared/interfaces';

@Component({
  selector: 'app-workspace-modal',
  imports: [InputComponent],
  templateUrl: './workspace-modal.component.html',
  styleUrl: './workspace-modal.component.scss',
})
export class WorkspaceModalComponent {
  public workspaceMember: WorkspaceMemberResponse | null = null;

  @Output() onEmitOwnerId = new EventEmitter<void>();

  constructor(private workspaceService: WorkspaceService) {}

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
    const request: WorkspaceRequest = {
      name: this.workspaceName.value,
    };
    this.workspaceService.createWorkspace(request).subscribe({
      next: (result) => {
        this.workspaceMember = result;
        this.onEmitOwnerId.emit();
      },

      error: (err) => {
        console.error(err);
      },
    });
  }
}
