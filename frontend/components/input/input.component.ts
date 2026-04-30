import { Component, effect, input, output, signal } from '@angular/core';
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
  public autocomplete = input<string>('off');
  public modelValue = input<string>('');
  public errorMessage = input<string>('');
  public hasError = input<boolean>(false);
  public isValid = input<boolean>(false);
  public value = signal<string>('');
  public isFocused = signal<boolean>(false);
  public onGetValue = output<string>();
  public onInputBlur = output<void>();

  constructor() {
    this.setupValue();
  }

  private setupValue() {
    effect(() => {
      this.value.set(this.modelValue() ?? '');
    });
  }

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
    this.onInputBlur.emit();
  }
}
