import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ThemeService, TenantBranding } from './theme.service';
import { environment } from '../../../environments/environment';

describe('ThemeService', () => {
  let service: ThemeService;
  let httpMock: HttpTestingController;

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

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });

    service = TestBed.inject(ThemeService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should default companyName to TeamForge when no branding', () => {
    expect(service.companyName()).toBe('TeamForge');
    expect(service.tagLine()).toBe('');
    expect(service.logoUrl()).toBeNull();
  });

  describe('loadBranding', () => {
    it('should fetch branding and apply theme', () => {
      vi.spyOn(service, 'applyTheme');
      service.loadBranding();

      const req = httpMock.expectOne(`${environment.apiUrl}/branding`);
      expect(req.request.method).toBe('GET');
      req.flush(mockBranding);

      expect(service.branding()).toEqual(mockBranding);
      expect(service.companyName()).toBe('Acme Corp');
      expect(service.tagLine()).toBe('Building the future');
      expect(service.isLoading()).toBe(false);
      expect(service.applyTheme).toHaveBeenCalledWith(mockBranding);
    });

    it('should set isLoading during fetch', () => {
      service.loadBranding();
      expect(service.isLoading()).toBe(true);

      httpMock.expectOne(`${environment.apiUrl}/branding`).flush(mockBranding);
      expect(service.isLoading()).toBe(false);
    });

    it('should handle fetch error gracefully', () => {
      service.loadBranding();

      httpMock.expectOne(`${environment.apiUrl}/branding`)
        .flush(null, { status: 500, statusText: 'Server Error' });

      expect(service.isLoading()).toBe(false);
      expect(service.branding()).toBeNull();
    });
  });

  describe('updateBranding', () => {
    it('should PUT updates and apply new theme', () => {
      vi.spyOn(service, 'applyTheme');
      const updated = { ...mockBranding, primaryColor: '#ff0000' };

      service.updateBranding({ primaryColor: '#ff0000' });

      const req = httpMock.expectOne(`${environment.apiUrl}/branding`);
      expect(req.request.method).toBe('PUT');
      req.flush(updated);

      expect(service.branding()).toEqual(updated);
      expect(service.applyTheme).toHaveBeenCalledWith(updated);
    });
  });

  describe('applyTheme', () => {
    it('should set CSS custom properties on document root', () => {
      service.applyTheme(mockBranding);

      const root = document.documentElement;
      expect(root.style.getPropertyValue('--tf-primary')).toBe('#1976d2');
      expect(root.style.getPropertyValue('--tf-secondary')).toBe('#ff9800');
      expect(root.style.getPropertyValue('--tf-accent')).toBe('#4caf50');
      expect(root.style.getPropertyValue('--tf-background')).toBe('#fafafa');
      expect(root.style.getPropertyValue('--tf-text')).toBe('#212121');
      expect(root.style.getPropertyValue('--tf-font-family')).toBe("'Roboto', sans-serif");
    });
  });

  describe('previewTheme', () => {
    it('should merge partial branding with current and apply', () => {
      service.branding.set(mockBranding);
      vi.spyOn(service, 'applyTheme');

      service.previewTheme({ primaryColor: '#ff0000' });

      expect(service.applyTheme).toHaveBeenCalledWith(
        expect.objectContaining({ primaryColor: '#ff0000', secondaryColor: '#ff9800' })
      );
    });

    it('should do nothing if no current branding', () => {
      vi.spyOn(service, 'applyTheme');
      service.previewTheme({ primaryColor: '#ff0000' });
      expect(service.applyTheme).not.toHaveBeenCalled();
    });
  });

  describe('resetTheme', () => {
    it('should re-apply current branding', () => {
      service.branding.set(mockBranding);
      vi.spyOn(service, 'applyTheme');

      service.resetTheme();

      expect(service.applyTheme).toHaveBeenCalledWith(mockBranding);
    });
  });

  describe('resetToDefaults', () => {
    it('should set default CSS custom properties', () => {
      service.resetToDefaults();

      const root = document.documentElement;
      expect(root.style.getPropertyValue('--tf-primary')).toBe('#1976d2');
      expect(root.style.getPropertyValue('--tf-secondary')).toBe('#ff9800');
      expect(root.style.getPropertyValue('--tf-accent')).toBe('#4caf50');
      expect(root.style.getPropertyValue('--tf-background')).toBe('#fafafa');
      expect(root.style.getPropertyValue('--tf-text')).toBe('#212121');
    });
  });
});
