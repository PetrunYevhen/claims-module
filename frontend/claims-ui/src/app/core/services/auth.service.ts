import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CurrentUser, LoginResponse, MockUserSummary } from '../models/auth.model';

const STORAGE_KEY = 'claims-ui.currentUser';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  readonly currentUser = signal<CurrentUser | null>(readFromStorage());
  readonly isAuthenticated = computed(() => this.currentUser() !== null);

  login(username: string, password: string): Observable<CurrentUser> {
    return this.http
      .post<LoginResponse>(`${environment.apiBaseUrl}/auth/login`, { username, password })
      .pipe(
        map(
          (response): CurrentUser => ({
            userId: response.userId,
            name: response.name,
            role: response.role,
            token: response.token
          })
        ),
        tap((user) => {
          localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
          this.currentUser.set(user);
        })
      );
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.currentUser.set(null);
  }

  listMockUsers(): Observable<MockUserSummary[]> {
    return this.http.get<MockUserSummary[]>(`${environment.apiBaseUrl}/auth/users`);
  }
}

function readFromStorage(): CurrentUser | null {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return null;
  }
  try {
    return JSON.parse(raw) as CurrentUser;
  } catch {
    return null;
  }
}
