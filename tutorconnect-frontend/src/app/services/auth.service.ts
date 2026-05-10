import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { DecodedToken } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/Auth`;
  private tokenKey = 'auth_token';

  constructor(private http: HttpClient) {}

  login(credentials: { email: string; password: string }): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/login`, credentials).pipe(
      tap(token => this.setToken(token))
    );
  }

  register(userData: any): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/register`, userData);
  }

  setToken(token: string): void {
    localStorage.setItem(this.tokenKey, token);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) return false;
    const decoded = this.decodeToken(token);
    if (!decoded) return false;
    // Check expiry
    return decoded.exp * 1000 > Date.now();
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
  }

  decodeToken(token: string): DecodedToken | null {
    try {
      const payload = token.split('.')[1];
      return JSON.parse(atob(payload));
    } catch {
      return null;
    }
  }

  getCurrentUserId(): number | null {
    const token = this.getToken();
    if (!token) return null;
    const decoded = this.decodeToken(token);
    if (!decoded) return null;
    const id = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
    return id ? parseInt(id, 10) : null;
  }

  getCurrentUserName(): string {
    const token = this.getToken();
    if (!token) return '';
    const decoded = this.decodeToken(token);
    return decoded?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ?? '';
  }

  getCurrentUserEmail(): string {
    const token = this.getToken();
    if (!token) return '';
    const decoded = this.decodeToken(token);
    return decoded?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ?? '';
  }

  getCurrentUserRole(): string {
    const token = this.getToken();
    if (!token) return '';
    const decoded = this.decodeToken(token);
    return decoded?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? '';
  }

  isAdmin(): boolean {
    return this.getCurrentUserRole() === 'Admin';
  }

  isTutor(): boolean {
    return this.getCurrentUserRole() === 'Tutor';
  }

  isStudent(): boolean {
    return this.getCurrentUserRole() === 'Student';
  }
}
