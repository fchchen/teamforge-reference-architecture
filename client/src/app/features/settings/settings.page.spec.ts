import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { SettingsPage } from './settings.page';
import { ThemeService, TenantBranding } from '../../core/services/theme.service';

describe('SettingsPage', () => {
  let component: SettingsPage;
  let fixture: ComponentFixture<SettingsPage>;
  let themeService: ThemeService;

  const mockBranding: TenantBranding = {
    tenantId: 'tenant-1',
    companyName: 'Acme Corp',
    primaryColor: '#1976d2',
    secondaryColor: '#ff9800',
    accentColor: '#4caf50',
    backgroundColor: '#fafafa',
    textColor: '#212121',
    logoUrl: null,
    fontFamily: 'Roboto',
    tagLine: 'Building the future'
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SettingsPage, HttpClientTestingModule, NoopAnimationsModule]
    }).compileComponents();

    themeService = TestBed.inject(ThemeService);
    fixture = TestBed.createComponent(SettingsPage);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should initialize with default colors when no branding', () => {
    fixture.detectChanges();
    expect(component.primaryColor()).toBe('#1976d2');
    expect(component.secondaryColor()).toBe('#ff9800');
    expect(component.fontFamily()).toBe('Roboto');
  });

  it('should load colors from branding on init', () => {
    themeService.branding.set(mockBranding);
    fixture.detectChanges();

    expect(component.primaryColor()).toBe('#1976d2');
    expect(component.tagLine()).toBe('Building the future');
    expect(component.fontFamily()).toBe('Roboto');
  });

  it('should update color signal on change', () => {
    spyOn(themeService, 'previewTheme');
    fixture.detectChanges();

    component.onColorChange('primaryColor', '#ff0000');
    expect(component.primaryColor()).toBe('#ff0000');
    expect(themeService.previewTheme).toHaveBeenCalled();
  });

  it('should update font and trigger preview', () => {
    spyOn(themeService, 'previewTheme');
    fixture.detectChanges();

    component.onFontChange('Inter');
    expect(component.fontFamily()).toBe('Inter');
    expect(themeService.previewTheme).toHaveBeenCalledWith(
      jasmine.objectContaining({ fontFamily: 'Inter' })
    );
  });

  it('should reset preview to saved branding', () => {
    themeService.branding.set(mockBranding);
    fixture.detectChanges();

    component.onColorChange('primaryColor', '#ff0000');
    expect(component.primaryColor()).toBe('#ff0000');

    spyOn(themeService, 'resetTheme');
    component.resetPreview();

    expect(component.primaryColor()).toBe('#1976d2');
    expect(themeService.resetTheme).toHaveBeenCalled();
  });

  it('should call updateBranding with current values on save', () => {
    spyOn(themeService, 'updateBranding');
    fixture.detectChanges();

    component.primaryColor.set('#ff0000');
    component.pendingTagLine.set('New tagline');
    component.saveBranding();

    expect(themeService.updateBranding).toHaveBeenCalledWith(
      jasmine.objectContaining({
        primaryColor: '#ff0000',
        tagLine: 'New tagline'
      })
    );
  });
});
