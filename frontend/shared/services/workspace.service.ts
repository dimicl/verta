import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { WorkspaceMemberResponse, WorkspaceRequest } from '../interfaces';

@Injectable({ providedIn: 'root' })
export class WorkspaceService {
  private API_URL = 'http://localhost:8080/api';
  private readonly token = localStorage.getItem('token');

  constructor(private http: HttpClient) {}

  public createWorkspace(request: WorkspaceRequest) {
    return this.http.post<WorkspaceMemberResponse>(
      `${this.API_URL}/workspace`,
      request,
      {
        headers: {
          Authorization: `Bearer ${this.token}`,
        },
      }
    );
  }

  public getWorkspace() {
    return this.http.get<any>(`${this.API_URL}/workspace/my`, {
      headers: {
        Authorization: `Bearer ${this.token}`,
      },
    });
  }

  public inviteUser(request: any) {
    return this.http.post<any>(`${this.API_URL}/workspace/invite`, request, {
      headers: {
        Authorization: `Bearer ${this.token}`,
      },
    });
  }
}
