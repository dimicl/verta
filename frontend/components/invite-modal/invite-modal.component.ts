import { Component } from '@angular/core';
import { InputComponent } from '../input/input.component';
import { FormControl, Validators } from '@angular/forms';

@Component({
  selector: 'app-invite-modal',
  imports: [InputComponent],
  templateUrl: './invite-modal.component.html',
  styleUrl: './invite-modal.component.scss',
})
export class InviteModalComponent {
  public workspaceName = new FormControl('', [Validators.required]);

  public onInput(event: string) {
    this.workspaceName.setValue(event);
  }
}
