import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { LoginPage } from './login.page';
import { AuthService } from '../../core/services/auth.service';

describe('LoginPage', () => {
  let component: LoginPage;
  let fixture: ComponentFixture<LoginPage>;
  let authService: AuthService;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [LoginPage, HttpClientTestingModule, RouterTestingModule, NoopAnimationsModule]
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
    expect(component.loginForm.contains('email')).toBeTrue();
    expect(component.loginForm.contains('password')).toBeTrue();
  });

  it('should start with invalid form', () => {
    expect(component.loginForm.valid).toBeFalse();
  });

  it('should be valid with proper email and password', () => {
    component.loginForm.setValue({ email: 'test@example.com', password: 'password123' });
    expect(component.loginForm.valid).toBeTrue();
  });

  it('should reject invalid email format', () => {
    component.loginForm.setValue({ email: 'not-an-email', password: 'password123' });
    expect(component.loginForm.get('email')?.hasError('email')).toBeTrue();
  });

  it('should render three demo buttons', () => {
    const buttons = fixture.nativeElement.querySelectorAll('.demo-btn');
    expect(buttons.length).toBe(3);
    expect(buttons[0].textContent).toContain('Acme Corp');
    expect(buttons[1].textContent).toContain('Pixel Studio');
    expect(buttons[2].textContent).toContain('GreenLeaf');
  });

  it('should call demoLogin with tenant name on demo button click', () => {
    spyOn(component, 'demoLogin');
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
});
