import { Routes } from '@angular/router';
import { MainComponent } from '../main/main.component';
import { AuthComponent } from '../auth/auth.component';
import { authGuard } from '../../shared/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },
  {
    path: 'main',
    component: MainComponent,
    canActivate: [authGuard],
  },
  {
    path: 'login',
    component: AuthComponent,
  },
  {
    path: 'register',
    component: AuthComponent,
  },
  {
    path: '**',
    redirectTo: 'login',
  },
];
