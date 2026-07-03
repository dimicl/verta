import { CommonModule } from '@angular/common';
import { Component, input, output, signal } from '@angular/core';
import { SvgIconComponent } from 'angular-svg-icon';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { BytesToHumanReadablePipe } from '../../shared/pipes/bytes-to-human-readable.pipe';
import { AppFile } from '../drop-zone/models/app-file.model';

@Component({
  selector: 'app-document-preview',
  imports: [CommonModule, SvgIconComponent, BytesToHumanReadablePipe],
  templateUrl: './document-preview.component.html',
  styleUrl: './document-preview.component.scss',
})
export class DocumentPreviewComponent {
  public sharedSvgRoutes = SharedSvgRoutes;

  public file = input.required<AppFile>();
  public onDelete = output<string>();
  public showDeleteModal = signal(false);

  public get fileType(): string {
    return this.file().type;
  }

  public get isPdf(): boolean {
    return this.fileType === 'pdf';
  }

  public get isVideo(): boolean {
    return ['avi', 'mp4', 'mov'].includes(this.fileType);
  }

  public get isImage(): boolean {
    return ['jpg', 'png', 'jpeg', 'gif'].includes(this.fileType);
  }

  public get previewUrl(): string {
    return this.file().imagePreviewUrl;
  }

  public handleDelete(event: Event): void {
    event.stopPropagation();
    this.showDeleteModal.set(true);
  }

  public confirmDelete(): void {
    this.onDelete.emit(this.file().fileName);
    this.showDeleteModal.set(false);
  }

  public cancelDelete(): void {
    this.showDeleteModal.set(false);
  }
}
