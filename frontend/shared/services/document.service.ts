import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  public async generateThumbnail(file: File): Promise<string> {
    if (file.type.startsWith('image/')) {
      return URL.createObjectURL(file);
    }

    return '';
  }
}
