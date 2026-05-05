import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class UserService {
  private API_URL = 'http://localhost:8080/api';
  constructor(private http: HttpClient) {}

  public getUserById(id: string) {
    return this.http.get(`${this.API_URL}/users/${id}`);
  }
}
