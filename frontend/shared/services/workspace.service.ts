import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { API_URL } from '../config/api.config';
import { WorkspaceMemberResponse, WorkspaceRequest } from '../interfaces';
import { WorkspaceResponse } from '../interfaces/workspace-response.interface';

@Injectable({ providedIn: 'root' })
export class WorkspaceService {
  private readonly apiUrl = API_URL;

  constructor(private http: HttpClient) {}

  private authHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      Authorization: `Bearer ${token ?? ''}`,
    });
  }

  public createWorkspace(request: WorkspaceRequest) {
    return this.http.post<WorkspaceMemberResponse>(
      `${this.apiUrl}/workspace`,
      request,
      {
        headers: this.authHeaders(),
      }
    );
  }

  public getWorkspace() {
    return this.http.get<any>(`${this.apiUrl}/workspace/my`, {
      headers: this.authHeaders(),
    });
  }

  public updateWorkspace(workspaceId: number, request: WorkspaceRequest) {
    return this.http.put<WorkspaceResponse>(
      `${this.apiUrl}/workspace/${workspaceId}`,
      request,
      { headers: this.authHeaders() }
    );
  }

  public deleteWorkspace(workspaceId: number) {
    return this.http.delete<void>(`${this.apiUrl}/workspace/${workspaceId}`, {
      headers: this.authHeaders(),
    });
  }

  public inviteUser(request: any) {
    return this.http.post<any>(`${this.apiUrl}/workspace/invite`, request, {
      headers: this.authHeaders(),
    });
  }

  public getMembers(workspaceId: number) {
    return this.http.get<any>(
      `${this.apiUrl}/workspace/${workspaceId}/members`,
      {
        headers: this.authHeaders(),
      }
    );
  }
}
