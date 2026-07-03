import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-avatar',
  imports: [NgbModule, CommonModule],
  templateUrl: './avatar.component.html',
  styleUrl: './avatar.component.scss',
})
export class AvatarComponent {
  readonly firstName = input('');
  readonly lastName = input('');
  readonly width = input(36);
  readonly height = input(36);
  readonly colorKey = input('');
  readonly dashboardType = input(false);
  readonly isSelected = input(false);

  private readonly palette = [
    'blue',
    'green',
    'red',
    'yellow',
    'purple',
    'gold',
    'light-green',
    'orange',
    'light-blue',
    'pink',
    'brown',
    'gray',
  ] as const;

  initials = computed(() => {
    const first = (this.firstName()?.trim()?.[0] ?? '').toUpperCase();
    const last = (this.lastName()?.trim()?.[0] ?? '').toUpperCase();
    return first + last || '?';
  });

  showPlaceholder = computed(() => {
    const init = this.initials();
    return init === '' || init === '?';
  });

  colorClass = computed(() => {
    if (this.colorKey()?.trim()) {
      return this.colorKey().trim().toLowerCase();
    }

    const seed = `${this.firstName()}${this.lastName()}`.trim().toLowerCase();
    if (!seed) return 'gray';

    let hash = 0;
    for (let i = 0; i < seed.length; i++) {
      hash = (hash * 31 + seed.charCodeAt(i)) >>> 0;
    }

    return this.palette[hash % this.palette.length];
  });
}
