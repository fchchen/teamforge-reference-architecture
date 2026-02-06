import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { authGuard, adminGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

describe('authGuard', () => {
  let authService: AuthService;
  let router: Router;

  const mockRoute = {} as ActivatedRouteSnapshot;
  const mockState = { url: '/dashboard' } as RouterStateSnapshot;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });

    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  afterEach(() => localStorage.clear());

  it('should redirect to login when unauthenticated', () => {
    const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));
    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toContain('/login');
  });

  it('should allow access when authenticated', () => {
    authService.authResponse.set({
      token: 'test',
      email: 'a@b.com',
      displayName: 'User',
      tenantId: 't1',
      tenantName: 'Acme',
      role: 'Member',
      expiration: new Date(Date.now() + 3600000).toISOString()
    });

    const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));
    expect(result).toBe(true);
  });

  it('should include returnUrl query param on redirect', () => {
    const state = { url: '/projects' } as RouterStateSnapshot;
    const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, state));
    expect((result as UrlTree).queryParams['returnUrl']).toBe('/projects');
  });
});

describe('adminGuard', () => {
  let authService: AuthService;

  const mockRoute = {} as ActivatedRouteSnapshot;
  const mockState = { url: '/settings' } as RouterStateSnapshot;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });

    authService = TestBed.inject(AuthService);
  });

  afterEach(() => localStorage.clear());

  it('should redirect to dashboard when not admin', () => {
    authService.authResponse.set({
      token: 'test',
      email: 'a@b.com',
      displayName: 'User',
      tenantId: 't1',
      tenantName: 'Acme',
      role: 'Member',
      expiration: new Date(Date.now() + 3600000).toISOString()
    });

    const result = TestBed.runInInjectionContext(() => adminGuard(mockRoute, mockState));
    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toContain('/dashboard');
  });

  it('should allow access for admin role', () => {
    authService.authResponse.set({
      token: 'test',
      email: 'a@b.com',
      displayName: 'Admin',
      tenantId: 't1',
      tenantName: 'Acme',
      role: 'Admin',
      expiration: new Date(Date.now() + 3600000).toISOString()
    });

    const result = TestBed.runInInjectionContext(() => adminGuard(mockRoute, mockState));
    expect(result).toBe(true);
  });

  it('should redirect when unauthenticated', () => {
    const result = TestBed.runInInjectionContext(() => adminGuard(mockRoute, mockState));
    expect(result).toBeInstanceOf(UrlTree);
  });
});
