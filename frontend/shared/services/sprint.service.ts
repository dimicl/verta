import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { SprintRequest, SprintResponse } from '../interfaces';

@Injectable({ providedIn: 'root' })
export class SprintService {
  private API_URL = 'http://localhost:8080/api/sprints';
  private readonly token = localStorage.getItem('token');

  constructor(private http: HttpClient) {}

  public create(request: SprintRequest) {
    return this.http.post<SprintResponse>(this.API_URL, request, {
      headers: {
        Authorization: `Bearer ${this.token}`,
      },
    });
  }

  public getByBoardId(boardId: number) {
    return this.http.get<SprintResponse[]>(`${this.API_URL}/board/${boardId}`, {
      headers: {
        Authorization: `Bearer ${this.token}`,
      },
    });
  }
}
