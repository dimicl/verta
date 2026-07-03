import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'bytesToHumanReadable',
  standalone: true,
})
export class BytesToHumanReadablePipe implements PipeTransform {
  public transform(bytes: number | null | undefined): string {
    if (!bytes || bytes <= 0) {
      return '0 B';
    }

    const units = ['B', 'KB', 'MB', 'GB'];
    let value = bytes;
    let unitIndex = 0;

    while (value >= 1024 && unitIndex < units.length - 1) {
      value /= 1024;
      unitIndex++;
    }

    return `${value.toFixed(unitIndex === 0 ? 0 : 1)} ${units[unitIndex]}`;
  }
}
