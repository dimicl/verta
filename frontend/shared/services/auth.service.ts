import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { API_URL } from '../config/api.config';
import { LoginRequest, RegisterRequest } from '../interfaces';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = API_URL;
  constructor(private httpClient: HttpClient) {}

  public register(registerRequest: RegisterRequest) {
    return this.httpClient.post<any>(
      `${this.apiUrl}/register`,
      registerRequest
    );
  }

  public login(loginRequest: LoginRequest) {
    return this.httpClient.post<any>(`${this.apiUrl}/login`, loginRequest);
  }
}