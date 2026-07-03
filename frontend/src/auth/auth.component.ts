import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, ReactiveFormsModule } from '@angular/forms';
import { FormGroup } from '@angular/forms';
import { AuthService } from '../../shared/services';
import { Router } from '@angular/router';
import { InputComponent } from '../../components/input/input.component';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { AuthHelper } from '../../shared/helpers/auth.helper';

@Component({
  selector: 'app-auth',
  imports: [CommonModule, ReactiveFormsModule, InputComponent],
  templateUrl: './auth.component.html',
  styleUrl: './auth.component.scss',
})
export class AuthComponent implements OnInit {
  public ngOnInit(): void {
    this.setupForm();
  }

  constructor(private authService: AuthService, private router: Router) {}

  public buttonText = signal<string>('');
  public subtitleText = signal<string>('');
  public isSubmitted = signal<boolean>(false);
  public loginError = signal<string>('');

  public sharedSvgRoutes = SharedSvgRoutes;
  public AuthHelper = AuthHelper;

  private fb = inject(FormBuilder);
  public loginForm: FormGroup = this.fb.group({});
  public registerForm: FormGroup = this.fb.group({});

  public get isRegisterMode(): boolean {
    return this.router.url.includes('/register');
  }

  private setupTitle() {
    if (this.isRegisterMode) {
      this.buttonText.set('Create account');
      this.subtitleText.set('Have an account? Sign in');
    } else {
      this.buttonText.set('Sign in');
      this.subtitleText.set('Need an account? Create one');
    }
  }

  public setupForm() {
    const { loginForm, registerForm } = AuthHelper.setupAuthForms(this.fb);
    this.loginForm = loginForm;
    this.registerForm = registerForm;

    this.setupTitle();
  }

  public onToggleAuthMode() {
    this.loginForm.reset();
    this.registerForm.reset();
    this.isSubmitted.set(false);
    this.loginError.set('');
    this.router.navigate([this.isRegisterMode ? '/login' : '/register']);
  }

  public getFieldError(controlName: string): string {
    const form = this.isRegisterMode ? this.registerForm : this.loginForm;
    const control = form.get(controlName);

    if (!control) {
      return '';
    }

    const shouldShowError =
      this.isSubmitted() || control.touched || control.dirty;

    if (!shouldShowError || !control.errors) {
      return '';
    }

    if (control.errors['required']) {
      return 'This field is required.';
    }

    if (control.errors['email']) {
      return 'Please enter a valid email address.';
    }

    if (control.errors['minlength']) {
      const requiredLength = control.errors['minlength'].requiredLength;
      return `Minimum length is ${requiredLength} characters.`;
    }

    if (control.errors['maxlength']) {
      const requiredLength = control.errors['maxlength'].requiredLength;
      return `Maximum length is ${requiredLength} characters.`;
    }

    if (control.errors['pattern']) {
      return 'Password must contain uppercase, lowercase, number and special character.';
    }

    if (control.errors['passwordMismatch']) {
      return 'Passwords do not match.';
    }

    return '';
  }

  public hasFieldError(controlName: string): boolean {
    const form = this.isRegisterMode ? this.registerForm : this.loginForm;
    const control = form.get(controlName);

    if (!control) {
      return false;
    }

    const shouldShowError =
      this.isSubmitted() || control.touched || control.dirty;

    return shouldShowError && control.invalid;
  }

  public isFieldValid(controlName: string): boolean {
    const form = this.isRegisterMode ? this.registerForm : this.loginForm;
    const control = form.get(controlName);

    if (!control) {
      return false;
    }

    const shouldShowState =
      this.isSubmitted() || control.touched || control.dirty;

    return shouldShowState && control.valid;
  }

  public onFieldValueChange(controlName: string, value: string): void {
    const form = this.isRegisterMode ? this.registerForm : this.loginForm;
    const control = form.get(controlName);

    if (!control) {
      return;
    }

    control.setValue(value);
    control.markAsDirty();
    control.updateValueAndValidity({ onlySelf: true });

    if (!this.isRegisterMode && (controlName === 'email' || controlName === 'password')) {
      this.loginError.set('');
    }
  }

  public onFieldBlur(controlName: string): void {
    const form = this.isRegisterMode ? this.registerForm : this.loginForm;
    const control = form.get(controlName);

    if (!control) {
      return;
    }

    control.markAsTouched();
    control.updateValueAndValidity({ onlySelf: true });
  }

  public onFormAction() {
    const form = this.isRegisterMode ? this.registerForm : this.loginForm;
    this.isSubmitted.set(true);
    form.markAllAsTouched();

    if (form.invalid) {
      return;
    }

    if (this.isRegisterMode) {
      this.authService
        .register(this.registerForm.value)
        .subscribe((response) => {
          localStorage.setItem('user_id', response.user.id);
          localStorage.setItem('token', response.token);
          this.registerForm.reset();
          this.router.navigate(['/main']);
        });
    } else {
      this.loginError.set('');
      this.authService.login(this.loginForm.value).subscribe({
        next: (response) => {
          localStorage.setItem('user_id', response.user.id);
          localStorage.setItem('token', response.token);
          this.loginForm.reset();
          this.router.navigate(['/main']);
        },
        error: (err) => {
          this.loginForm.reset();
          this.isSubmitted.set(false);
          this.loginError.set(
            err.error?.message ?? 'Login failed. Please try again.'
          );
        },
      });
    }
  }
}
