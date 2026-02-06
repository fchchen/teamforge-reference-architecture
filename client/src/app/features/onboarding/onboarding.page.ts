import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatStepperModule } from '@angular/material/stepper';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { catchError, tap } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';

interface OnboardingPreview {
  branding: {
    primaryColor: string;
    secondaryColor: string;
    accentColor: string;
    backgroundColor: string;
    textColor: string;
    fontFamily: string;
    tagLine: string;
  };
  roles: string[];
  teams: { name: string; description: string }[];
  projectCategories: string[];
  welcomeAnnouncement: string;
}

@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule,
    MatStepperModule, MatChipsModule, MatProgressBarModule,
    MatListModule, MatIconModule
  ],
  templateUrl: './onboarding.page.html',
  styleUrl: './onboarding.page.scss'
})
export class OnboardingPage {
  private http = inject(HttpClient);
  private router = inject(Router);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);
  private apiUrl = environment.apiUrl;

  descriptionForm = this.fb.group({
    description: ['', [Validators.required, Validators.minLength(20)]]
  });

  preview = signal<OnboardingPreview | null>(null);
  isGenerating = signal(false);
  isConfirming = signal(false);

  generate(): void {
    if (this.descriptionForm.invalid) return;

    this.isGenerating.set(true);
    this.http.post<OnboardingPreview>(`${this.apiUrl}/onboarding/generate`, {
      companyName: this.authService.tenantName(),
      companyDescription: this.descriptionForm.value.description
    }).pipe(
      tap(preview => {
        this.preview.set(preview);
        this.isGenerating.set(false);
      }),
      catchError(() => {
        this.isGenerating.set(false);
        return of(null);
      })
    ).subscribe();
  }

  confirm(): void {
    const p = this.preview();
    if (!p) return;

    this.isConfirming.set(true);
    this.http.post(`${this.apiUrl}/onboarding/confirm`, {
      tenantId: this.authService.tenantId(),
      config: p
    }).pipe(
      tap(() => {
        this.isConfirming.set(false);
      }),
      catchError(() => {
        this.isConfirming.set(false);
        return of(null);
      })
    ).subscribe();
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }
}
