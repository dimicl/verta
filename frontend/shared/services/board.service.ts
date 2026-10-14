import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { API_URL } from '../config/api.config';
import {
  BoardInviteRequest,
  BoardRequest,
  BoardResponse,
} from '../interfaces';

@Injectable({ providedIn: 'root' })
export class BoardService {
  private readonly apiUrl = `${API_URL}/boards`;

  constructor(private http: HttpClient) {}

  private authHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      Authorization: `Bearer ${token ?? ''}`,
    });
  }

  public createBoard(request: BoardRequest) {
    return this.http.post<BoardResponse>(`${this.apiUrl}`, request, {
      headers: this.authHeaders(),
    });
  }

  public getBoards(workspaceId: number | undefined) {
    return this.http.get<BoardResponse[]>(`${this.apiUrl}/workspace/${workspaceId}`, {
      headers: this.authHeaders(),
    });
  }

  public getById(boardId: number) {
    return this.http.get<BoardResponse>(`${this.apiUrl}/${boardId}`, {
      headers: this.authHeaders(),
    });
  }

  public getBoardMembers(boardId: number) {
    return this.http.get<any[]>(`${this.apiUrl}/${boardId}/members`, {
      headers: this.authHeaders(),
    });
  }

  public inviteToBoard(request: BoardInviteRequest) {
    return this.http.post<{ message: string }>(`${this.apiUrl}/invite`, request, {
      headers: this.authHeaders(),
    });
  }
}
