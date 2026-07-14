import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  ChangeWorkItemStatusRequest,
  CommentRequest,
  WorkItemLockResponse,
  WorkItemRequest,
  WorkItemResponse,
} from '../interfaces';
import { TaskStatus } from '../types/task-status.type';
import { TaskPriority } from '../types/task-priority.type';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private API_URL = 'http://localhost:8080/api/work-items';
  private LOCK_API_URL = 'http://localhost:8080/api/work-item-locks';
  private COMMENT_API_URL = 'http://localhost:8080/api/comments';

  constructor(private http: HttpClient) {}

  private authHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      Authorization: `Bearer ${token ?? ''}`,
    });
  }

  public getByBoardId(boardId: number) {
    return this.http.get<WorkItemResponse[]>(`${this.API_URL}/board/${boardId}`, {
      headers: this.authHeaders(),
    });
  }

  public getById(workItemId: number) {
    return this.http.get<WorkItemResponse>(`${this.API_URL}/${workItemId}`, {
      headers: this.authHeaders(),
    });
  }

  public create(request: WorkItemRequest) {
    return this.http.post<WorkItemResponse>(this.API_URL, request, {
      headers: this.authHeaders(),
    });
  }

  public update(workItemId: number, request: WorkItemRequest) {
    return this.http.put<WorkItemResponse>(`${this.API_URL}/${workItemId}`, request, {
      headers: this.authHeaders(),
    });
  }

  public delete(workItemId: number) {
    return this.http.delete<void>(`${this.API_URL}/${workItemId}`, {
      headers: this.authHeaders(),
    });
  }

  public changeStatus(workItemId: number, status: TaskStatus) {
    const payload: ChangeWorkItemStatusRequest = { status };
    return this.http.patch<WorkItemResponse>(
      `${this.API_URL}/${workItemId}/status`,
      payload,
      {
        headers: this.authHeaders(),
      }
    );
  }

  public changePriority(workItemId: number, priority: TaskPriority) {
    return this.http.patch<WorkItemResponse>(
      `${this.API_URL}/${workItemId}/priority`,
      { priority },
      {
        headers: this.authHeaders(),
      }
    );
  }

  public changeAssignee(workItemId: number, assignedUserId: number | null) {
    return this.http.patch<WorkItemResponse>(
      `${this.API_URL}/${workItemId}/assignee`,
      { assignedUserId },
      {
        headers: this.authHeaders(),
      }
    );
  }

  public openWorkItem(workItemId: number) {
    return this.http.post<WorkItemLockResponse>(
      `${this.LOCK_API_URL}/open/${workItemId}`,
      null,
      {
        headers: this.authHeaders(),
      }
    );
  }

  public closeWorkItem(workItemId: number) {
    return this.http.post<WorkItemLockResponse>(
      `${this.LOCK_API_URL}/close/${workItemId}`,
      null,
      {
        headers: this.authHeaders(),
      }
    );
  }

  public heartbeatWorkItem(workItemId: number) {
    return this.http.post<WorkItemLockResponse>(
      `${this.LOCK_API_URL}/heartbeat/${workItemId}`,
      null,
      {
        headers: this.authHeaders(),
      }
    );
  }

  public createComment(request: CommentRequest) {
    return this.http.post(this.COMMENT_API_URL, request, {
      headers: this.authHeaders(),
    });
  }
}
