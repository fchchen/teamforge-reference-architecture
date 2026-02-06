import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { tap, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';

interface TeamMember {
  userId: string;
  displayName: string;
  email: string;
  role: string;
}

interface Team {
  id: string;
  name: string;
  description: string | null;
  memberCount: number;
  members: TeamMember[];
  createdAt: string;
}

@Component({
  selector: 'app-teams',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatListModule,
    MatChipsModule, MatProgressSpinnerModule
  ],
  templateUrl: './teams.page.html',
  styleUrl: './teams.page.scss'
})
export class TeamsPage implements OnInit {
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);
  private apiUrl = environment.apiUrl;

  teams = signal<Team[]>([]);
  isLoading = signal(true);
  showCreateForm = signal(false);

  createForm = this.fb.group({
    name: ['', Validators.required],
    description: ['']
  });

  ngOnInit(): void {
    this.http.get<Team[]>(`${this.apiUrl}/teams`).pipe(
      tap(teams => {
        this.teams.set(teams);
        this.isLoading.set(false);
      }),
      catchError(() => {
        this.isLoading.set(false);
        return of([]);
      })
    ).subscribe();
  }

  createTeam(): void {
    if (this.createForm.invalid) return;
    const v = this.createForm.value;
    this.http.post<Team>(`${this.apiUrl}/teams`, {
      name: v.name,
      description: v.description || null
    }).pipe(
      tap(team => {
        this.teams.update(list => [...list, team]);
        this.createForm.reset();
        this.showCreateForm.set(false);
      }),
      catchError(() => of(null))
    ).subscribe();
  }
}
