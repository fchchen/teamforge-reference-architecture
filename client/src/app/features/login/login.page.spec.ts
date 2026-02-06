import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { LoginPage } from './login.page';
import { AuthService } from '../../core/services/auth.service';
import { MsalService } from '../../core/services/msal.service';

describe('LoginPage', () => {
  let component: LoginPage;
  let fixture: ComponentFixture<LoginPage>;
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
      imports: [LoginPage, HttpClientTestingModule, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: MsalService, useValue: mockMsalService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginPage);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService);
    fixture.detectChanges();
  });

  afterEach(() => localStorage.clear());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render TeamForge title', () => {
    const title = fixture.nativeElement.querySelector('mat-card-title');
    expect(title.textContent).toContain('TeamForge');
  });

  it('should have email and password fields', () => {
    expect(component.loginForm.contains('email')).toBe(true);
    expect(component.loginForm.contains('password')).toBe(true);
  });

  it('should start with invalid form', () => {
    expect(component.loginForm.valid).toBe(false);
  });

  it('should be valid with proper email and password', () => {
    component.loginForm.setValue({ email: 'test@example.com', password: 'password123' });
    expect(component.loginForm.valid).toBe(true);
  });

  it('should reject invalid email format', () => {
    component.loginForm.setValue({ email: 'not-an-email', password: 'password123' });
    expect(component.loginForm.get('email')?.hasError('email')).toBe(true);
  });

  it('should render three demo buttons', () => {
    const buttons = fixture.nativeElement.querySelectorAll('.demo-btn');
    expect(buttons.length).toBe(3);
    expect(buttons[0].textContent).toContain('Acme Corp');
    expect(buttons[1].textContent).toContain('Pixel Studio');
    expect(buttons[2].textContent).toContain('GreenLeaf');
  });

  it('should call demoLogin with tenant name on demo button click', () => {
    vi.spyOn(component, 'demoLogin');
    const buttons = fixture.nativeElement.querySelectorAll('.demo-btn');
    buttons[0].click();
    expect(component.demoLogin).toHaveBeenCalledWith('Acme Corp');
  });

  it('should show error message when auth error exists', () => {
    authService.error.set('Invalid credentials');
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('.error-message');
    expect(errorEl.textContent).toContain('Invalid credentials');
  });

  it('should have link to register page', () => {
    const link = fixture.nativeElement.querySelector('a[routerLink="/register"]');
    expect(link).toBeTruthy();
    expect(link.textContent).toContain('Create a new workspace');
  });

  it('should render Sign in with Microsoft button', () => {
    const btn = fixture.nativeElement.querySelector('.microsoft-btn');
    expect(btn).toBeTruthy();
    expect(btn.textContent).toContain('Sign in with Microsoft');
  });

  it('should call signInWithMicrosoft on Microsoft button click', () => {
    vi.spyOn(component, 'signInWithMicrosoft');
    const btn = fixture.nativeElement.querySelector('.microsoft-btn');
    btn.click();
    expect(component.signInWithMicrosoft).toHaveBeenCalled();
  });
});
