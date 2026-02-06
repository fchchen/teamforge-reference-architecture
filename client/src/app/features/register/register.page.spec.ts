import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { RegisterPage } from './register.page';

describe('RegisterPage', () => {
  let component: RegisterPage;
  let fixture: ComponentFixture<RegisterPage>;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [RegisterPage, HttpClientTestingModule, RouterTestingModule, NoopAnimationsModule]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => localStorage.clear());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render Create Your Workspace title', () => {
    const title = fixture.nativeElement.querySelector('mat-card-title');
    expect(title.textContent).toContain('Create Your Workspace');
  });

  it('should have all required form fields', () => {
    expect(component.registerForm.contains('companyName')).toBeTrue();
    expect(component.registerForm.contains('displayName')).toBeTrue();
    expect(component.registerForm.contains('email')).toBeTrue();
    expect(component.registerForm.contains('password')).toBeTrue();
  });

  it('should start with invalid form', () => {
    expect(component.registerForm.valid).toBeFalse();
  });

  it('should be valid with proper values', () => {
    component.registerForm.setValue({
      companyName: 'New Company',
      displayName: 'Jane Doe',
      email: 'jane@newco.com',
      password: 'password123'
    });
    expect(component.registerForm.valid).toBeTrue();
  });

  it('should require minimum 2 characters for company name', () => {
    component.registerForm.patchValue({ companyName: 'A' });
    expect(component.registerForm.get('companyName')?.hasError('minlength')).toBeTrue();
  });

  it('should require minimum 8 characters for password', () => {
    component.registerForm.patchValue({ password: 'short' });
    expect(component.registerForm.get('password')?.hasError('minlength')).toBeTrue();
  });

  it('should have link to login page', () => {
    const link = fixture.nativeElement.querySelector('a[routerLink="/login"]');
    expect(link).toBeTruthy();
    expect(link.textContent).toContain('Already have a workspace');
  });
});
