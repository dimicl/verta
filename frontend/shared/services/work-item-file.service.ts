import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { WorkItemFileResponse } from '../interfaces/work-item-file-response.interface';

@Injectable({ providedIn: 'root' })
export class WorkItemFileService {
  private readonly apiUrl = 'http://localhost:8080/api/work-item-files';

  constructor(private readonly http: HttpClient) {}

  private authHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      Authorization: `Bearer ${token ?? ''}`,
    });
  }

  public upload(workItemId: number, file: File) {
    const formData = new FormData();
    formData.append('workItemId', String(workItemId));
    formData.append('file', file, file.name);

    return this.http.post<WorkItemFileResponse>(`${this.apiUrl}/upload`, formData, {
      headers: this.authHeaders(),
    });
  }

  public getByWorkItemId(workItemId: number) {
    return this.http.get<WorkItemFileResponse[]>(
      `${this.apiUrl}/work-item/${workItemId}`,
      { headers: this.authHeaders() }
    );
  }

  public delete(fileId: number) {
    return this.http.delete<void>(`${this.apiUrl}/${fileId}`, {
      headers: this.authHeaders(),
    });
  }
}
