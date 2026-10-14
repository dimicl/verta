import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TaskService } from '../services/task.service';

/**
 * Root-scoped tracker so logout / 401 can release a work-item lock
 * before the JWT is cleared (TaskSessionService is only provided on Main).
 */
@Injectable({ providedIn: 'root' })
export class ActiveWorkItemLockTracker {
  private workItemId: number | null = null;

  set(workItemId: number | null): void {
    this.workItemId = workItemId;
  }

  get(): number | null {
    return this.workItemId;
  }

  release(taskService: TaskService): Observable<unknown> {
    const id = this.workItemId;
    this.workItemId = null;
    if (id === null) {
      return of(null);
    }
    return taskService.closeWorkItem(id).pipe(catchError(() => of(null)));
  }
}
