import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { SprintRequest, SprintResponse, UpdateSprintRequest } from '../interfaces';
import { API_URL } from '../config/api.config';

export type { UpdateSprintRequest } from '../interfaces';

@Injectable({ providedIn: 'root' })
export class SprintService {
  private readonly apiUrl = `${API_URL}/sprints`;

  constructor(private http: HttpClient) {}

  public create(request: SprintRequest) {
    return this.http.post<SprintResponse>(this.apiUrl, request);
  }

  public getById(sprintId: number) {
    return this.http.get<SprintResponse>(`${this.apiUrl}/${sprintId}`);
  }

  public getByBoardId(boardId: number) {
    return this.http.get<SprintResponse[]>(`${this.apiUrl}/board/${boardId}`);
  }

  public update(sprintId: number, request: UpdateSprintRequest) {
    return this.http.put<SprintResponse>(`${this.apiUrl}/${sprintId}`, request);
  }

  public delete(sprintId: number) {
    return this.http.delete<void>(`${this.apiUrl}/${sprintId}`);
  }
}
