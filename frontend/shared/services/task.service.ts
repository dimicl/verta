import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { API_URL } from '../config/api.config';
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
  private readonly apiUrl = `${API_URL}/work-items`;
  private readonly lockApiUrl = `${API_URL}/work-item-locks`;
  private readonly commentApiUrl = `${API_URL}/comments`;

  constructor(private http: HttpClient) {}

  private authHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      Authorization: `Bearer ${token ?? ''}`,
    });
  }

  public getByBoardId(boardId: number) {
    return this.http.get<WorkItemResponse[]>(`${this.apiUrl}/board/${boardId}`, {
      headers: this.authHeaders(),
    });
  }

  public getBySprintId(sprintId: number) {
    return this.http.get<WorkItemResponse[]>(`${this.apiUrl}/sprint/${sprintId}`, {
      headers: this.authHeaders(),
    });
  }

  public getById(workItemId: number) {
    return this.http.get<WorkItemResponse>(`${this.apiUrl}/${workItemId}`, {
      headers: this.authHeaders(),
    });
  }

  public create(request: WorkItemRequest) {
    return this.http.post<WorkItemResponse>(this.apiUrl, request, {
      headers: this.authHeaders(),
    });
  }

  public update(workItemId: number, request: WorkItemRequest) {
    return this.http.put<WorkItemResponse>(`${this.apiUrl}/${workItemId}`, request, {
      headers: this.authHeaders(),
    });
  }

  public delete(workItemId: number) {
    return this.http.delete<void>(`${this.apiUrl}/${workItemId}`, {
      headers: this.authHeaders(),
    });
  }

  public changeStatus(workItemId: number, status: TaskStatus) {
    const payload: ChangeWorkItemStatusRequest = { status };
    return this.http.patch<WorkItemResponse>(
      `${this.apiUrl}/${workItemId}/status`,
      payload,
      {
        headers: this.authHeaders(),
      }
    );
  }

  public changePriority(workItemId: number, priority: TaskPriority) {
    return this.http.patch<WorkItemResponse>(
      `${this.apiUrl}/${workItemId}/priority`,
      { priority },
      {
        headers: this.authHeaders(),
      }
    );
  }

  public changeAssignee(workItemId: number, assignedUserId: number | null) {
    return this.http.patch<WorkItemResponse>(
      `${this.apiUrl}/${workItemId}/assignee`,
      { assignedUserId },
      {
        headers: this.authHeaders(),
      }
    );
  }

  public openWorkItem(workItemId: number) {
    return this.http.post<WorkItemLockResponse>(
      `${this.lockApiUrl}/open/${workItemId}`,
      null,
      {
        headers: this.authHeaders(),
      }
    );
  }

  public closeWorkItem(workItemId: number) {
    return this.http.post<WorkItemLockResponse>(
      `${this.lockApiUrl}/close/${workItemId}`,
      null,
      {
        headers: this.authHeaders(),
      }
    );
  }

  public heartbeatWorkItem(workItemId: number) {
    return this.http.post<WorkItemLockResponse>(
      `${this.lockApiUrl}/heartbeat/${workItemId}`,
      null,
      {
        headers: this.authHeaders(),
      }
    );
  }

  public createComment(request: CommentRequest) {
    return this.http.post(this.commentApiUrl, request, {
      headers: this.authHeaders(),
    });
  }
}
