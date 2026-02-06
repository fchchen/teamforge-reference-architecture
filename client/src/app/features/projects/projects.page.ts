import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { tap, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';

interface Project {
  id: string;
  name: string;
  description: string | null;
  status: string;
  category: string | null;
  createdAt: string;
}

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule,
    MatChipsModule, MatProgressSpinnerModule
  ],
  templateUrl: './projects.page.html',
  styleUrl: './projects.page.scss'
})
export class ProjectsPage implements OnInit {
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);
  private apiUrl = environment.apiUrl;

  projects = signal<Project[]>([]);
  isLoading = signal(true);
  showCreateForm = signal(false);

  createForm = this.fb.group({
    name: ['', Validators.required],
    description: [''],
    category: ['']
  });

  ngOnInit(): void {
    this.loadProjects();
  }

  loadProjects(): void {
    this.http.get<Project[]>(`${this.apiUrl}/projects`).pipe(
      tap(projects => {
        this.projects.set(projects);
        this.isLoading.set(false);
      }),
      catchError(() => {
        this.isLoading.set(false);
        return of([]);
      })
    ).subscribe();
  }

  createProject(): void {
    if (this.createForm.invalid) return;
    const v = this.createForm.value;
    this.http.post<Project>(`${this.apiUrl}/projects`, {
      name: v.name,
      description: v.description || null,
      category: v.category || null
    }).pipe(
      tap(project => {
        this.projects.update(list => [project, ...list]);
        this.createForm.reset();
        this.showCreateForm.set(false);
      }),
      catchError(() => of(null))
    ).subscribe();
  }

  deleteProject(id: string): void {
    this.http.delete(`${this.apiUrl}/projects/${id}`).pipe(
      tap(() => {
        this.projects.update(list => list.filter(p => p.id !== id));
      }),
      catchError(() => of(null))
    ).subscribe();
  }
}
