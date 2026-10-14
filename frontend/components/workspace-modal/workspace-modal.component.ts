import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output, signal } from '@angular/core';
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
import { WorkspaceResponse } from '../../shared/interfaces/workspace-response.interface';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-workspace-modal',
  imports: [CommonModule, InputComponent],
  templateUrl: './workspace-modal.component.html',
  styleUrl: './workspace-modal.component.scss',
})
export class WorkspaceModalComponent implements OnInit {
  @Input() public workspace: WorkspaceResponse | null = null;
  @Input() public mode: 'create' | 'edit' = 'create';

  public workspaceMember: WorkspaceMemberResponse | null = null;
  public formError = signal('');
  public isSaving = signal(false);

  @Output() onEmitOwnerId = new EventEmitter<void>();

  constructor(
    private workspaceService: WorkspaceService,
    private activeModal: NgbActiveModal
  ) {}

  public get isEditMode(): boolean {
    return this.mode === 'edit' && this.workspace != null;
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

  public ngOnInit(): void {
    if (this.workspace?.name) {
      this.workspaceName.setValue(this.workspace.name);
    }
  }

  public onInput(event: string) {
    this.workspaceName.setValue(event);
    this.formError.set('');
  }

  public onCreate() {
    if (this.workspaceName.invalid) {
      this.formError.set('Enter a valid workspace name (min 5 chars, no digits).');
      return;
    }

    const request: WorkspaceRequest = {
      name: this.workspaceName.value,
    };

    this.isSaving.set(true);

    if (this.isEditMode && this.workspace) {
      this.workspaceService.updateWorkspace(this.workspace.id, request).subscribe({
        next: (result) => {
          this.isSaving.set(false);
          this.activeModal.close({ action: 'updated', workspace: result });
        },
        error: (err) => {
          this.isSaving.set(false);
          this.formError.set(err.error?.message ?? 'Update failed.');
        },
      });
      return;
    }

    this.workspaceService.createWorkspace(request).subscribe({
      next: (result) => {
        this.isSaving.set(false);
        this.workspaceMember = result;
        this.onEmitOwnerId.emit();
        this.activeModal.close({ action: 'created' });
      },
      error: (err) => {
        this.isSaving.set(false);
        this.formError.set(err.error?.message ?? 'Create failed.');
        console.error(err);
      },
    });
  }

  public onDelete(): void {
    if (!this.workspace) {
      return;
    }

    if (
      !confirm(
        `Delete workspace "${this.workspace.name}"? This cannot be undone.`
      )
    ) {
      return;
    }

    this.isSaving.set(true);
    this.workspaceService.deleteWorkspace(this.workspace.id).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.activeModal.close({ action: 'deleted' });
      },
      error: (err) => {
        this.isSaving.set(false);
        this.formError.set(err.error?.message ?? 'Delete failed.');
      },
    });
  }

  public onClose(): void {
    this.activeModal.dismiss();
  }
}
