import { Component, inject, effect, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { AuthService } from './core/services/auth.service';
import { ThemeService } from './core/services/theme.service';
import { LoadingService } from './core/services/loading.service';
import { ThemedToolbarComponent } from './shared/components/themed-toolbar.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, MatProgressBarModule, ThemedToolbarComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  authService = inject(AuthService);
  loadingService = inject(LoadingService);
  private themeService = inject(ThemeService);

  constructor() {
    // Load branding when user authenticates
    effect(() => {
      const isAuth = this.authService.isAuthenticated();
      if (isAuth) {
        untracked(() => this.themeService.loadBranding());
      } else {
        untracked(() => this.themeService.resetToDefaults());
      }
    }, { allowSignalWrites: true });
  }
}
