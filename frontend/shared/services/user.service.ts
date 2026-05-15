import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class UserService {
  private API_URL = 'http://localhost:8080/api';
  constructor(private http: HttpClient) {}

  public getUserById() {
    const id = localStorage.getItem('user_id');
    return this.http.get(`${this.API_URL}/users/${id}`);
  }
}
