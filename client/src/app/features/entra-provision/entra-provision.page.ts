import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-entra-provision',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatProgressBarModule, MatIconModule
  ],
  templateUrl: './entra-provision.page.html',
  styles: [`
    .provision-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background-color: var(--tf-background);
    }
    .provision-card {
      width: 100%;
      max-width: 420px;
      padding: 24px;
    }
    .full-width { width: 100%; }
    .error-message {
      background: #ffebee;
      color: #c62828;
      padding: 12px;
      border-radius: 4px;
      margin-bottom: 16px;
    }
  `]
})
export class EntraProvisionPage {
  authService = inject(AuthService);
  private fb = inject(FormBuilder);

  provisionForm = this.fb.group({
    companyName: ['', [Validators.required, Validators.maxLength(200)]],
    displayName: ['', [Validators.required, Validators.maxLength(100)]]
  });

  onSubmit(): void {
    if (this.provisionForm.valid) {
      const { companyName, displayName } = this.provisionForm.value;
      this.authService.entraProvision(companyName!, displayName!);
    }
  }
}
