import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  ChangeSubWorkItemStatusRequest,
  SubWorkItemRequest,
  SubWorkItemResponse,
  UpdateSubWorkItemRequest,
} from '../interfaces';
import { TaskStatus } from '../types/task-status.type';

@Injectable({ providedIn: 'root' })
export class SubWorkItemService {
  private readonly apiUrl = 'http://localhost:8080/api/sub-work-items';

  constructor(private readonly http: HttpClient) {}

  private authHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      Authorization: `Bearer ${token ?? ''}`,
    });
  }

  public getById(subWorkItemId: number) {
    return this.http.get<SubWorkItemResponse>(`${this.apiUrl}/${subWorkItemId}`, {
      headers: this.authHeaders(),
    });
  }

  public getByWorkItemId(workItemId: number) {
    return this.http.get<SubWorkItemResponse[]>(
      `${this.apiUrl}/work-item/${workItemId}`,
      { headers: this.authHeaders() }
    );
  }

  public create(request: SubWorkItemRequest) {
    return this.http.post<SubWorkItemResponse>(this.apiUrl, request, {
      headers: this.authHeaders(),
    });
  }

  public update(subWorkItemId: number, request: UpdateSubWorkItemRequest) {
    return this.http.put<SubWorkItemResponse>(
      `${this.apiUrl}/${subWorkItemId}`,
      request,
      { headers: this.authHeaders() }
    );
  }

  public changeStatus(subWorkItemId: number, status: TaskStatus) {
    const payload: ChangeSubWorkItemStatusRequest = { status };
    return this.http.patch<SubWorkItemResponse>(
      `${this.apiUrl}/${subWorkItemId}/status`,
      payload,
      { headers: this.authHeaders() }
    );
  }

  public delete(subWorkItemId: number) {
    return this.http.delete<void>(`${this.apiUrl}/${subWorkItemId}`, {
      headers: this.authHeaders(),
    });
  }
}
