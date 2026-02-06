import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { ThemeService, TenantBranding } from '../../core/services/theme.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatCardModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatIconModule, MatDividerModule
  ],
  templateUrl: './settings.page.html',
  styleUrl: './settings.page.scss'
})
export class SettingsPage implements OnInit {
  themeService = inject(ThemeService);

  primaryColor = signal('#1976d2');
  secondaryColor = signal('#ff9800');
  accentColor = signal('#4caf50');
  backgroundColor = signal('#fafafa');
  textColor = signal('#212121');
  fontFamily = signal('Roboto');
  tagLine = signal('');
  pendingTagLine = signal('');

  ngOnInit(): void {
    const b = this.themeService.branding();
    if (b) {
      this.primaryColor.set(b.primaryColor);
      this.secondaryColor.set(b.secondaryColor);
      this.accentColor.set(b.accentColor);
      this.backgroundColor.set(b.backgroundColor);
      this.textColor.set(b.textColor);
      this.fontFamily.set(b.fontFamily);
      this.tagLine.set(b.tagLine ?? '');
      this.pendingTagLine.set(b.tagLine ?? '');
    }
  }

  onColorChange(property: string, value: string): void {
    switch (property) {
      case 'primaryColor': this.primaryColor.set(value); break;
      case 'secondaryColor': this.secondaryColor.set(value); break;
      case 'accentColor': this.accentColor.set(value); break;
      case 'backgroundColor': this.backgroundColor.set(value); break;
      case 'textColor': this.textColor.set(value); break;
    }
    // Live preview
    this.themeService.previewTheme({ [property]: value } as Partial<TenantBranding>);
  }

  onFontChange(value: string): void {
    this.fontFamily.set(value);
    this.themeService.previewTheme({ fontFamily: value } as Partial<TenantBranding>);
  }

  resetPreview(): void {
    this.themeService.resetTheme();
    const b = this.themeService.branding();
    if (b) {
      this.primaryColor.set(b.primaryColor);
      this.secondaryColor.set(b.secondaryColor);
      this.accentColor.set(b.accentColor);
      this.backgroundColor.set(b.backgroundColor);
      this.textColor.set(b.textColor);
      this.fontFamily.set(b.fontFamily);
    }
  }

  saveBranding(): void {
    this.themeService.updateBranding({
      primaryColor: this.primaryColor(),
      secondaryColor: this.secondaryColor(),
      accentColor: this.accentColor(),
      backgroundColor: this.backgroundColor(),
      textColor: this.textColor(),
      fontFamily: this.fontFamily(),
      tagLine: this.pendingTagLine()
    });
  }
}
