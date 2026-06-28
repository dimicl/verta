import { Component, EventEmitter, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { WorkspaceService } from '../../shared/services';
/* import { WorkspaceService } from '../services/workspace.service';
 */
@Component({
  selector: 'app-create-workspace-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-workspace-modal.component.html',
  styleUrl: './create-workspace-modal.component.scss',
})
export class CreateWorkspaceModalComponent {
  public model = {
    name: '',
  };

  public isLoading = false;

  @Output() closeModal = new EventEmitter<void>();
  @Output() created = new EventEmitter<any>();

  constructor(private workspaceService: WorkspaceService) {}

  public close(): void {
    this.closeModal.emit();
  }

  public create(): void {
    if (!this.model.name.trim()) return;

    this.isLoading = true;

    const payload = {
      name: this.model.name,
      ownerId: this.getCurrentUserId(),
    };

    this.workspaceService.createWorkspace(payload).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.created.emit(res);
        this.close();
      },
      error: (err) => {
        console.error('Create workspace failed', err);
        this.isLoading = false;
      },
    });
  }

  private getCurrentUserId(): number {
    // kasnije zameni sa JWT / auth store
    return Number(localStorage.getItem('userId'));
  }
}
