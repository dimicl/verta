import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { API_URL } from '../config/api.config';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly apiUrl = API_URL;
  constructor(private http: HttpClient) {}

  public getUserById() {
    return this.http.get(`${this.apiUrl}/users/me`);
  }

  public getById(userId: number) {
    return this.http.get(`${this.apiUrl}/users/${userId}`);
  }
}
