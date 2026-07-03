import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  BoardInviteRequest,
  BoardRequest,
  BoardResponse,
} from '../interfaces';

@Injectable({ providedIn: 'root' })
export class BoardService {
  private API_URL = 'http://localhost:8080/api/boards';
  private readonly token = localStorage.getItem('token');

  constructor(private http: HttpClient) {}

  public createBoard(request: BoardRequest) {
    return this.http.post<BoardResponse>(`${this.API_URL}`, request, {
      headers: {
        Authorization: `Bearer ${this.token}`,
      },
    });
  }

  public getBoards(workspaceId: number | undefined) {
    return this.http.get<BoardResponse[]>(`${this.API_URL}/workspace/${workspaceId}`, {
      headers: {
        Authorization: `Bearer ${this.token}`,
      },
    });
  }

  public getBoardMembers(boardId: number) {
    return this.http.get<any[]>(`${this.API_URL}/${boardId}/members`, {
      headers: {
        Authorization: `Bearer ${this.token}`,
      },
    });
  }

  public inviteToBoard(request: BoardInviteRequest) {
    return this.http.post<{ message: string }>(`${this.API_URL}/invite`, request, {
      headers: {
        Authorization: `Bearer ${this.token}`,
      },
    });
  }
}
