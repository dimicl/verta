import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { SignalRService } from '../services/signalr.service';
import { TaskService } from '../services/task.service';
import { ActiveWorkItemLockTracker } from '../services/active-work-item-lock.tracker';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const signalRService = inject(SignalRService);
  const taskService = inject(TaskService);
  const lockTracker = inject(ActiveWorkItemLockTracker);

  const token = localStorage.getItem('token');

  const authReq = token
    ? req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      })
    : req;

  return next(authReq).pipe(
    catchError((err) => {
      if (err.status === 401) {
        // Release lock while JWT is still available, then clear session.
        return lockTracker.release(taskService).pipe(
          switchMap(() => {
            localStorage.removeItem('token');
            localStorage.removeItem('user_id');
            void signalRService.disconnect();
            router.navigate(['/login']);
            return throwError(() => err);
          }),
          catchError(() => {
            localStorage.removeItem('token');
            localStorage.removeItem('user_id');
            void signalRService.disconnect();
            router.navigate(['/login']);
            return throwError(() => err);
          })
        );
      }

      return throwError(() => err);
    })
  );
};
