import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { tap, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ThemeService } from '../../core/services/theme.service';

interface DashboardData {
  projectCount: number;
  teamCount: number;
  userCount: number;
  recentAnnouncements: { id: string; title: string; content: string; createdByName: string; createdAt: string }[];
  recentProjects: { id: string; name: string; description: string; status: string; category: string; createdAt: string }[];
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatCardModule, MatButtonModule, MatIconModule, MatListModule, MatProgressSpinnerModule
  ],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss'
})
export class DashboardPage implements OnInit {
  private http = inject(HttpClient);
  themeService = inject(ThemeService);
  private apiUrl = environment.apiUrl;

  data = signal<DashboardData | null>(null);
  isLoading = signal(true);

  ngOnInit(): void {
    this.http.get<DashboardData>(`${this.apiUrl}/dashboard`).pipe(
      tap(data => {
        this.data.set(data);
        this.isLoading.set(false);
      }),
      catchError(() => {
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe();
  }
}
