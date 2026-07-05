import { CommonModule } from '@angular/common';
import {
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  Input,
  OnChanges,
  SimpleChanges,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SvgIconComponent } from 'angular-svg-icon';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import {
  fileExtensionFromName,
  resolveFileUrl,
} from '../../shared/interfaces/work-item-file-response.interface';
import { DocumentService } from '../../shared/services/document.service';
import { WorkItemFileService } from '../../shared/services/work-item-file.service';
import { DocumentPreviewComponent } from '../document-preview/document-preview.component';
import { AppFile, FileExtension } from './models/app-file.model';

const SUPPORTED_TYPES: FileExtension[] = [
  'pdf',
  'png',
  'jpg',
  'jpeg',
  'gif',
  'avi',
  'mov',
  'mp4',
];

@Component({
  selector: 'app-drop-zone',
  imports: [SvgIconComponent, CommonModule, DocumentPreviewComponent],
  templateUrl: './drop-zone.component.html',
  styleUrl: './drop-zone.component.scss',
})
export class DropZoneComponent implements OnChanges {
  @Input() public workItemId: number | null = null;
  @Input() public subWorkItemId: number | null | undefined = undefined;

  @ViewChild('fileInput') private fileInput?: ElementRef<HTMLInputElement>;
  @ViewChild('dropZoneContainer') private dropZoneContainer?: ElementRef<HTMLElement>;

  public sharedSvgRoutes = SharedSvgRoutes;
  public docs = signal<AppFile[]>([]);
  public onDragover = signal(false);
  public unsupported = signal(false);
  public invalidFileSize = signal(false);
  public isUploading = signal(false);
  public errorMessage = signal('');
  public carouselIndex = signal(0);

  private clientFileId = 0;
  private readonly maxFileSizeBytes = 100 * 1024 * 1024;
  private readonly visibleCount = 2;
  private readonly cardWidth = 148;
  private readonly cardGap = 10;

  public carouselTransform = computed(() => {
    const offset = this.carouselIndex() * -(this.cardWidth + this.cardGap);
    return `translateX(${offset}px)`;
  });

  public canCarouselLeft = computed(() => this.carouselIndex() > 0);

  public canCarouselRight = computed(() => {
    const maxIndex = Math.max(0, this.docs().length - this.visibleCount);
    return this.carouselIndex() < maxIndex;
  });

  public carouselViewportWidth = computed(() => {
    const count = Math.min(this.docs().length, this.visibleCount);
    if (count === 0) {
      return 0;
    }

    return count * this.cardWidth + Math.max(0, count - 1) * this.cardGap;
  });

  private readonly documentService = inject(DocumentService);
  private readonly workItemFileService = inject(WorkItemFileService);
  private readonly destroyRef = inject(DestroyRef);

  public ngOnChanges(changes: SimpleChanges): void {
    if (
      (changes['workItemId'] || changes['subWorkItemId']) &&
      this.canPersistFiles()
    ) {
      this.loadFiles();
    }
  }

  private canPersistFiles(): boolean {
    if (!this.workItemId) {
      return false;
    }

    if (this.subWorkItemId === undefined) {
      return true;
    }

    return this.subWorkItemId !== null;
  }

  public get pendingFiles(): File[] {
    return this.docs()
      .filter((doc) => doc.pending && doc.file)
      .map((doc) => doc.file as File);
  }

  public get fileCount(): number {
    return this.docs().length;
  }

  public onInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    if (target.files?.length) {
      this.processFiles(target.files);
    }
  }

  public handleClickToAdd(): void {
    this.fileInput?.nativeElement.click();
  }

  public carouselLeft(event: Event): void {
    event.stopPropagation();
    this.carouselIndex.update((index) => Math.max(0, index - 1));
  }

  public carouselRight(event: Event): void {
    event.stopPropagation();
    const maxIndex = Math.max(0, this.docs().length - this.visibleCount);
    this.carouselIndex.update((index) => Math.min(maxIndex, index + 1));
  }

  public onDrop(event: DragEvent): void {
    event.preventDefault();
    this.onDragover.set(false);

    const files = event.dataTransfer?.files;
    if (files?.length) {
      this.processFiles(files);
    }
  }

  public onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.onDragover.set(true);
  }

  public onDragLeave(event: DragEvent): void {
    event.preventDefault();
    const relatedTarget = event.relatedTarget as Node | null;
    const container = this.dropZoneContainer?.nativeElement;

    if (!container || !relatedTarget || !container.contains(relatedTarget)) {
      this.onDragover.set(false);
    }
  }

  @HostListener('document:dragover', ['$event'])
  public onDocumentDragOver(event: DragEvent): void {
    if (event.dataTransfer?.types.includes('Files')) {
      event.preventDefault();
    }
  }

  public cancelUnsupported(): void {
    this.unsupported.set(false);
    this.invalidFileSize.set(false);
    this.resetInput();
  }

  public handleDelete(fileName: string): void {
    const doc = this.docs().find((item) => item.fileName === fileName);
    if (!doc) {
      return;
    }

    if (doc.pending || doc.fileId < 0) {
      this.docs.update((items) => items.filter((item) => item.fileName !== fileName));
      this.clampCarouselIndex();
      return;
    }

    this.isUploading.set(true);
    this.workItemFileService
      .delete(doc.fileId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.docs.update((items) => items.filter((item) => item.fileName !== fileName));
          this.clampCarouselIndex();
          this.isUploading.set(false);
        },
        error: (err) => {
          this.errorMessage.set(err.error?.message ?? 'Failed to delete file.');
          this.isUploading.set(false);
        },
      });
  }

  private loadFiles(): void {
    if (!this.canPersistFiles() || !this.workItemId) {
      return;
    }

    const request =
      this.subWorkItemId !== undefined && this.subWorkItemId !== null
        ? this.workItemFileService.getBySubWorkItemId(this.subWorkItemId)
        : this.workItemFileService.getByWorkItemId(this.workItemId);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (files) => {
          this.docs.set(files.map((file) => this.mapApiFile(file)));
        },
        error: (err) => {
          this.errorMessage.set(err.error?.message ?? 'Failed to load files.');
        },
      });
  }

  private async processFiles(files: FileList): Promise<void> {
    this.unsupported.set(false);
    this.invalidFileSize.set(false);
    this.errorMessage.set('');

    const existingNames = new Set(this.docs().map((doc) => doc.fileName));

    for (const file of Array.from(files)) {
      const extension = file.name.split('.').pop()?.toLowerCase() as FileExtension;

      if (!extension || !SUPPORTED_TYPES.includes(extension)) {
        this.unsupported.set(true);
        continue;
      }

      if (file.size > this.maxFileSizeBytes) {
        this.invalidFileSize.set(true);
        return;
      }

      if (existingNames.has(file.name)) {
        continue;
      }

      existingNames.add(file.name);

      const previewUrl = await this.documentService.generateThumbnail(file);
      const localDoc: AppFile = {
        fileId: -++this.clientFileId,
        fileName: file.name,
        type: extension,
        size: file.size,
        imagePreviewUrl: previewUrl,
        file,
        pending: !this.canPersistFiles(),
      };

      this.docs.update((items) => [...items, localDoc]);

      if (this.canPersistFiles()) {
        await this.uploadFile(localDoc, file);
      }
    }

    this.syncCarouselToEnd();
    this.resetInput();
  }

  private uploadFile(localDoc: AppFile, file: File): Promise<void> {
    if (!this.canPersistFiles() || !this.workItemId) {
      return Promise.resolve();
    }

    this.isUploading.set(true);

    return new Promise((resolve) => {
      this.workItemFileService
        .upload(
          this.workItemId!,
          file,
          this.subWorkItemId !== undefined ? this.subWorkItemId : undefined
        )
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (created) => {
            this.docs.update((items) =>
              items.map((item) =>
                item.fileId === localDoc.fileId ? this.mapApiFile(created) : item
              )
            );
            this.isUploading.set(false);
            resolve();
          },
          error: (err) => {
            this.docs.update((items) =>
              items.filter((item) => item.fileId !== localDoc.fileId)
            );
            this.errorMessage.set(err.error?.message ?? 'Failed to upload file.');
            this.isUploading.set(false);
            resolve();
          },
        });
    });
  }

  public async uploadPendingFiles(workItemId: number): Promise<void> {
    const pending = this.docs().filter((doc) => doc.pending && doc.file);

    for (const doc of pending) {
      const file = doc.file as File;
      await this.uploadFile(doc, file);
    }
  }

  private mapApiFile(file: {
    id: number;
    fileName: string;
    fileSize: number;
    fileUrl: string;
    fileThumbnailUrl?: string | null;
  }): AppFile {
    const type = fileExtensionFromName(file.fileName) as FileExtension;
    const thumbnail = file.fileThumbnailUrl
      ? resolveFileUrl(file.fileThumbnailUrl)
      : resolveFileUrl(file.fileUrl);

    return {
      fileId: file.id,
      fileName: file.fileName,
      type: SUPPORTED_TYPES.includes(type) ? type : 'pdf',
      size: file.fileSize,
      imagePreviewUrl: ['jpg', 'jpeg', 'png', 'gif'].includes(type)
        ? thumbnail
        : '',
      pending: false,
    };
  }

  private resetInput(): void {
    if (this.fileInput?.nativeElement) {
      this.fileInput.nativeElement.value = '';
    }
  }

  private clampCarouselIndex(): void {
    const maxIndex = Math.max(0, this.docs().length - this.visibleCount);
    if (this.carouselIndex() > maxIndex) {
      this.carouselIndex.set(maxIndex);
    }
  }

  private syncCarouselToEnd(): void {
    const maxIndex = Math.max(0, this.docs().length - this.visibleCount);
    this.carouselIndex.set(maxIndex);
  }
}
