import { Component, input, output, signal } from '@angular/core';
import { SvgIconComponent } from 'angular-svg-icon';

@Component({
  selector: 'app-input',
  imports: [SvgIconComponent],
  templateUrl: './input.component.html',
  styleUrl: './input.component.scss',
})
export class InputComponent {
  public icon = input<string>();
  public type = input<string>('text');
  public label = input<string>('');
  public placeholder = input<string>('');
  public value = signal<string>('');
  public isFocused = signal<boolean>(false);

  public onGetValue = output<string>();

  public onInput(event: Event) {
    const input = event.target as HTMLInputElement;
    this.value.set(input.value);
    this.onGetValue.emit(input.value);
  }

  public onFocus() {
    this.isFocused.set(true);
  }

  public onBlur() {
    this.isFocused.set(false);
  }
}
