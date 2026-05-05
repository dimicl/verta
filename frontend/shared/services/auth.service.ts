import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { LoginRequest, RegisterRequest } from '../interfaces';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private API_URL = 'http://localhost:8080/api';
  constructor(private httpClient: HttpClient) {}

  public register(registerRequest: RegisterRequest) {
    return this.httpClient.post<any>(
      `${this.API_URL}/register`,
      registerRequest
    );
  }

  public login(loginRequest: LoginRequest) {
    return this.httpClient.post<any>(`${this.API_URL}/login`, loginRequest);
  }
}
