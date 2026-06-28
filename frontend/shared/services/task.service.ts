import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  ChangeWorkItemStatusRequest,
  CommentRequest,
  WorkItemLockResponse,
  WorkItemRequest,
  WorkItemResponse,
} from '../interfaces';
import { TaskStatus } from '../types/task-status.type';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private API_URL = 'http://localhost:8080/api/work-items';
  private LOCK_API_URL = 'http://localhost:8080/api/work-item-locks';
  private COMMENT_API_URL = 'http://localhost:8080/api/comments';
  private readonly token = localStorage.getItem('token');

  constructor(private http: HttpClient) {}

  public getByBoardId(boardId: number) {
    return this.http.get<WorkItemResponse[]>(`${this.API_URL}/board/${boardId}`, {
      headers: {
        Authorization: `Bearer ${this.token}`,
      },
    });
  }

  public create(request: WorkItemRequest) {
    return this.http.post<WorkItemResponse>(this.API_URL, request, {
      headers: {
        Authorization: `Bearer ${this.token}`,
      },
    });
  }

  public update(workItemId: number, request: WorkItemRequest) {
    return this.http.put<WorkItemResponse>(`${this.API_URL}/${workItemId}`, request, {
      headers: {
        Authorization: `Bearer ${this.token}`,
      },
    });
  }

  public delete(workItemId: number) {
    return this.http.delete<void>(`${this.API_URL}/${workItemId}`, {
      headers: {
        Authorization: `Bearer ${this.token}`,
      },
    });
  }

  public changeStatus(workItemId: number, status: TaskStatus) {
    const payload: ChangeWorkItemStatusRequest = { status };
    return this.http.patch<WorkItemResponse>(
      `${this.API_URL}/${workItemId}/status`,
      payload,
      {
        headers: {
          Authorization: `Bearer ${this.token}`,
        },
      }
    );
  }

  public openWorkItem(workItemId: number) {
    return this.http.post<WorkItemLockResponse>(
      `${this.LOCK_API_URL}/open/${workItemId}`,
      null,
      {
        headers: {
          Authorization: `Bearer ${this.token}`,
        },
      }
    );
  }

  public closeWorkItem(workItemId: number) {
    return this.http.post<WorkItemLockResponse>(
      `${this.LOCK_API_URL}/close/${workItemId}`,
      null,
      {
        headers: {
          Authorization: `Bearer ${this.token}`,
        },
      }
    );
  }

  public createComment(request: CommentRequest) {
    return this.http.post(this.COMMENT_API_URL, request, {
      headers: {
        Authorization: `Bearer ${this.token}`,
      },
    });
  }
}
