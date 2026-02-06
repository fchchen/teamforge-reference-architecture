import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { EntraProvisionPage } from './entra-provision.page';
import { AuthService } from '../../core/services/auth.service';
import { MsalService } from '../../core/services/msal.service';

describe('EntraProvisionPage', () => {
  let component: EntraProvisionPage;
  let fixture: ComponentFixture<EntraProvisionPage>;
  let authService: AuthService;

  beforeEach(async () => {
    localStorage.clear();

    const mockMsalService = {
      initialize: vi.fn().mockResolvedValue(undefined),
      loginPopup: vi.fn().mockResolvedValue('mock-token'),
      acquireTokenSilent: vi.fn().mockResolvedValue('mock-token'),
      logout: vi.fn().mockResolvedValue(undefined)
    };

    await TestBed.configureTestingModule({
      imports: [EntraProvisionPage, HttpClientTestingModule, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: MsalService, useValue: mockMsalService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(EntraProvisionPage);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService);
    fixture.detectChanges();
  });

  afterEach(() => localStorage.clear());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render workspace creation title', () => {
    const title = fixture.nativeElement.querySelector('mat-card-title');
    expect(title.textContent).toContain('Create Your Workspace');
  });

  it('should have companyName and displayName fields', () => {
    expect(component.provisionForm.contains('companyName')).toBe(true);
    expect(component.provisionForm.contains('displayName')).toBe(true);
  });

  it('should start with invalid form', () => {
    expect(component.provisionForm.valid).toBe(false);
  });

  it('should be valid with company name and display name', () => {
    component.provisionForm.setValue({ companyName: 'Test Corp', displayName: 'Test User' });
    expect(component.provisionForm.valid).toBe(true);
  });

  it('should call entraProvision on submit', () => {
    vi.spyOn(authService, 'entraProvision');
    component.provisionForm.setValue({ companyName: 'Test Corp', displayName: 'Test User' });
    component.onSubmit();
    expect(authService.entraProvision).toHaveBeenCalledWith('Test Corp', 'Test User');
  });

  it('should show error message when auth error exists', () => {
    authService.error.set('Provisioning failed');
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('.error-message');
    expect(errorEl.textContent).toContain('Provisioning failed');
  });

  it('should have link back to login', () => {
    const link = fixture.nativeElement.querySelector('a[routerLink="/login"]');
    expect(link).toBeTruthy();
    expect(link.textContent).toContain('Back to login');
  });
});
