import { Component, EventEmitter, Output } from '@angular/core';
import { InputComponent } from '../input/input.component';
import {
  AbstractControl,
  FormControl,
  ValidationErrors,
  Validators,
} from '@angular/forms';

@Component({
  selector: 'app-board-modal',
  imports: [InputComponent],
  templateUrl: './board-modal.component.html',
  styleUrl: './board-modal.component.scss',
})
export class BoardModalComponent {
  public workspaceMember: any | null = null;

  @Output() onEmitOwnerId = new EventEmitter<void>();

  constructor() {}

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
    const request = {
      name: this.workspaceName.value,
    };
  }
}
