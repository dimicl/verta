import { Component } from '@angular/core';
import { InputComponent } from '../input/input.component';
import { FormControl, Validators } from '@angular/forms';
import { WorkspaceService } from '../../shared/services';

@Component({
  selector: 'app-invite-modal',
  imports: [InputComponent],
  templateUrl: './invite-modal.component.html',
  styleUrl: './invite-modal.component.scss',
})
export class InviteModalComponent {
  public invitedEmail = new FormControl('', [
    Validators.required,
    Validators.email,
  ]);
  public workspaceId: number | null = null;

  constructor(private workspaceService: WorkspaceService) {}

  public onInput(event: string) {
    this.invitedEmail.setValue(event);
  }

  public onInviteUser() {
    const request: any = {
      WorkspaceId: this.workspaceId,
      Email: this.invitedEmail.value,
    };

    this.workspaceService.inviteUser(request).subscribe({
      next: (result) => {
        console.log(result);
      },
    });
  }
}
