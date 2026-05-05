import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const token = localStorage.getItem('token');
  const userId = localStorage.getItem('user_id');

  if (!token || !userId) {
    localStorage.removeItem('token');
    localStorage.removeItem('user_id');
    return router.createUrlTree(['/login']);
  }

  return true;
};
