import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgbActiveModal, NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { SvgIconComponent } from 'angular-svg-icon';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';

@Component({
  selector: 'app-confirm-modal',
  standalone: true,
  imports: [CommonModule, NgbModule, SvgIconComponent],
  templateUrl: './confirm-modal.component.html',
  styleUrl: './confirm-modal.component.scss',
})
export class ConfirmModalComponent {
  public sharedSvgRoutes = SharedSvgRoutes;

  @Input() public title = 'Confirm';
  @Input() public message = 'Are you sure?';
  @Input() public confirmLabel = 'Delete';
  @Input() public cancelLabel = 'Cancel';

  constructor(private readonly activeModal: NgbActiveModal) {}

  public onConfirm(): void {
    this.activeModal.close(true);
  }

  public onCancel(): void {
    this.activeModal.dismiss(false);
  }
}
