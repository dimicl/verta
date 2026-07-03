import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { CommentRequest } from '../interfaces/comment-request.interface';
import { CommentResponse } from '../interfaces/comment-response.interface';

@Injectable({ providedIn: 'root' })
export class CommentService {
  private readonly apiUrl = 'http://localhost:8080/api/comments';

  constructor(private readonly http: HttpClient) {}

  private authHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      Authorization: `Bearer ${token ?? ''}`,
    });
  }

  public getByWorkItemId(workItemId: number) {
    return this.http.get<CommentResponse[]>(
      `${this.apiUrl}/work-item/${workItemId}`,
      { headers: this.authHeaders() }
    );
  }

  public create(request: CommentRequest) {
    return this.http.post<CommentResponse>(this.apiUrl, request, {
      headers: this.authHeaders(),
    });
  }

  public update(commentId: number, content: string) {
    return this.http.put<CommentResponse>(
      `${this.apiUrl}/${commentId}`,
      { content },
      { headers: this.authHeaders() }
    );
  }

  public delete(commentId: number) {
    return this.http.delete<void>(`${this.apiUrl}/${commentId}`, {
      headers: this.authHeaders(),
    });
  }
}
