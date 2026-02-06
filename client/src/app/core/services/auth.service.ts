import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, tap } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';

const STORAGE_KEY = 'tf_auth';

export interface AuthResponse {
  token: string;
  email: string;
  displayName: string;
  tenantId: string;
  tenantName: string;
  role: string;
  expiration: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  companyName: string;
  email: string;
  displayName: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private apiUrl = environment.apiUrl;

  authResponse = signal<AuthResponse | null>(null);
  error = signal<string | null>(null);
  isLoading = signal(false);

  isAuthenticated = computed(() => {
    const auth = this.authResponse();
    if (!auth) return false;
    return new Date(auth.expiration) > new Date();
  });

  currentUser = computed(() => this.authResponse()?.displayName ?? null);
  token = computed(() => this.authResponse()?.token ?? null);
  tenantId = computed(() => this.authResponse()?.tenantId ?? null);
  tenantName = computed(() => this.authResponse()?.tenantName ?? null);
  role = computed(() => this.authResponse()?.role ?? null);

  constructor() {
    this.hydrateFromStorage();
  }

  login(request: LoginRequest): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, request).pipe(
      tap(response => {
        this.setAuth(response);
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      }),
      catchError(err => {
        this.error.set(err.error?.title || 'Invalid credentials');
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe();
  }

  register(request: RegisterRequest): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.http.post<AuthResponse>(`${this.apiUrl}/auth/register`, request).pipe(
      tap(response => {
        this.setAuth(response);
        this.isLoading.set(false);
        this.router.navigate(['/onboarding']);
      }),
      catchError(err => {
        this.error.set(err.error?.title || 'Registration failed');
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe();
  }

  demoLogin(tenantName: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.http.post<AuthResponse>(`${this.apiUrl}/auth/demo`, { tenantName }).pipe(
      tap(response => {
        this.setAuth(response);
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      }),
      catchError(err => {
        this.error.set(err.error?.title || 'Demo login failed');
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe();
  }

  logout(): void {
    this.authResponse.set(null);
    this.error.set(null);
    localStorage.removeItem(STORAGE_KEY);
    this.router.navigate(['/login']);
  }

  private setAuth(response: AuthResponse): void {
    this.authResponse.set(response);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(response));
  }

  private hydrateFromStorage(): void {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) return;
    try {
      const auth: AuthResponse = JSON.parse(stored);
      if (new Date(auth.expiration) > new Date()) {
        this.authResponse.set(auth);
      } else {
        localStorage.removeItem(STORAGE_KEY);
      }
    } catch {
      localStorage.removeItem(STORAGE_KEY);
    }
  }
}
