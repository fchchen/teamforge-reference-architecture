import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, tap } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface TenantBranding {
  tenantId: string;
  companyName: string;
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
  backgroundColor: string;
  textColor: string;
  logoUrl: string | null;
  fontFamily: string;
  tagLine: string | null;
}

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  branding = signal<TenantBranding | null>(null);
  isLoading = signal(false);

  companyName = computed(() => this.branding()?.companyName ?? 'TeamForge');
  tagLine = computed(() => this.branding()?.tagLine ?? '');
  logoUrl = computed(() => this.branding()?.logoUrl ?? null);

  loadBranding(): void {
    this.isLoading.set(true);

    this.http.get<TenantBranding>(`${this.apiUrl}/branding`).pipe(
      tap(branding => {
        this.branding.set(branding);
        this.applyTheme(branding);
        this.isLoading.set(false);
      }),
      catchError(() => {
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe();
  }

  updateBranding(updates: Partial<TenantBranding>): void {
    this.http.put<TenantBranding>(`${this.apiUrl}/branding`, updates).pipe(
      tap(branding => {
        this.branding.set(branding);
        this.applyTheme(branding);
      }),
      catchError(() => of(null))
    ).subscribe();
  }

  applyTheme(branding: TenantBranding): void {
    const root = document.documentElement;
    root.style.setProperty('--tf-primary', branding.primaryColor);
    root.style.setProperty('--tf-secondary', branding.secondaryColor);
    root.style.setProperty('--tf-accent', branding.accentColor);
    root.style.setProperty('--tf-background', branding.backgroundColor);
    root.style.setProperty('--tf-text', branding.textColor);
    root.style.setProperty('--tf-font-family', `'${branding.fontFamily}', sans-serif`);
  }

  previewTheme(branding: Partial<TenantBranding>): void {
    const current = this.branding();
    if (!current) return;
    this.applyTheme({ ...current, ...branding });
  }

  resetTheme(): void {
    const current = this.branding();
    if (current) this.applyTheme(current);
  }

  resetToDefaults(): void {
    const root = document.documentElement;
    root.style.setProperty('--tf-primary', '#1976d2');
    root.style.setProperty('--tf-secondary', '#ff9800');
    root.style.setProperty('--tf-accent', '#4caf50');
    root.style.setProperty('--tf-background', '#fafafa');
    root.style.setProperty('--tf-text', '#212121');
    root.style.setProperty('--tf-font-family', "'Roboto', sans-serif");
  }
}
