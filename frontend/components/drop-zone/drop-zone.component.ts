import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { SvgIconComponent } from 'angular-svg-icon';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { FileResponse } from '../../shared/models';

@Component({
  selector: 'app-drop-zone',
  imports: [SvgIconComponent, CommonModule],
  templateUrl: './drop-zone.component.html',
  styleUrl: './drop-zone.component.scss',
})
export class DropZoneComponent {
  public sharedSvgRoutes = SharedSvgRoutes;

  public docs = signal<FileResponse[]>([]);

  public onInput(event: Event) {
    const target = event.target as HTMLInputElement;
    const files = target.files as FileList;

    this.processFiles(files);
    console.log(files);
  }

  public async processFiles(files: FileList) {
    const newFiles: FileResponse[] = [];

    for (let i = 0; i < files.length; i++) {
      const file = files[i];

      const fileUrl = URL.createObjectURL(file);

      const isImage = file.type.startsWith('image');

      const fileResponse: FileResponse = {
        fileName: file.name,
        fileType: file.type,
        fileUrl: fileUrl,
        fileThumbail: isImage ? fileUrl : '', // za sad samo slike
      };

      newFiles.push(fileResponse);
    }

    // dodaj na postojeće fajlove
    this.docs.update((current) => [...current, ...newFiles]);
    console.log(this.docs());
  }
}
