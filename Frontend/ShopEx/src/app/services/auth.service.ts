import { API_URL } from '../core/api-config';
import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AppUser, LoginRequest, LoginResponse, UserRole } from '../models/models';

interface StoredSession {
  token: string;
  expiresAt: string;
  user: AppUser;
}

const STORAGE_KEY = 'shopex.session';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  /** Base URL of the API — also used by the interceptor to decide where the token may be sent. */
  static readonly apiUrl = API_URL;

  private readonly session = signal<StoredSession | null>(this.restore());

  readonly currentUser = computed(() => this.session()?.user ?? null);
  readonly isAdmin = computed(() => this.session()?.user.role === UserRole.Admin);
  readonly isEmployee = computed(() => this.session()?.user.role === UserRole.Employee);

  constructor(private http: HttpClient, private router: Router) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${AuthService.apiUrl}/Auth/login`, {
        username: request.username.trim(),
        password: request.password
      })
      .pipe(
        tap((res) => {
          const stored: StoredSession = { token: res.token, expiresAt: res.expiresAt, user: res.user };
          this.session.set(stored);
          this.persist(stored);
        })
      );
  }

  /** Clears the session; optionally sends the user to the login page. */
  logout(redirect = true, returnUrl?: string): void {
    this.session.set(null);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      /* storage unavailable */
    }
    if (redirect) {
      this.router.navigate(['/login'], returnUrl ? { queryParams: { returnUrl } } : undefined);
    }
  }

  /** Returns the JWT, or null when there is no session or it has expired. */
  getToken(): string | null {
    const s = this.session();
    if (!s) return null;
    if (this.isExpired(s)) {
      this.logout(false);
      return null;
    }
    return s.token;
  }

  isLoggedIn(): boolean {
    return this.getToken() !== null;
  }

  /** The page each role lands on after login. */
  homeRoute(): string {
    return this.isEmployee() ? '/branch/receive' : '/dashboard';
  }

  private isExpired(s: StoredSession): boolean {
    const exp = Date.parse(s.expiresAt);
    // a 10s margin avoids sending a token that expires mid-flight
    return Number.isNaN(exp) || exp - 10_000 <= Date.now();
  }

  private persist(s: StoredSession): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(s));
    } catch {
      /* storage unavailable: the session still works until the page reloads */
    }
  }

  private restore(): StoredSession | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const s = JSON.parse(raw) as StoredSession;
      if (!s?.token || !s.user || this.isExpired(s)) {
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }
      return s;
    } catch {
      return null;
    }
  }
}
