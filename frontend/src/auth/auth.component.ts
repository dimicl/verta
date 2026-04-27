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
      this.subtitleText.set('Need an account? Create one');
    } else {
      this.buttonText.set('Sign in');
      this.subtitleText.set('Have an account? Sign in');
    }
  }

  public setupForm() {
    const { loginForm, registerForm } = AuthHelper.setupAuthForms(this.fb);
    this.loginForm = loginForm;
    this.registerForm = registerForm;

    this.setupTitle();
  }

  public onToggleAuthMode() {
    this.router.navigate([this.isRegisterMode ? '/login' : '/register']);
  }

  public onFormAction() {
    if (this.isRegisterMode) {
      this.authService
        .register(this.registerForm.value)
        .subscribe((response) => {
          console.log(response);
          this.router.navigate(['/main']);
        });
    } else {
      this.authService.login(this.loginForm.value).subscribe((response) => {
        console.log(response);
      });
    }
  }
}
