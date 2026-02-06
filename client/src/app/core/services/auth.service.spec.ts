import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService, AuthResponse } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let router: jasmine.SpyObj<Router>;

  const mockAuthResponse: AuthResponse = {
    token: 'test-token-123',
    email: 'admin@acme.com',
    displayName: 'Admin User',
    tenantId: 'tenant-1',
    tenantName: 'Acme Corp',
    role: 'Admin',
    expiration: new Date(Date.now() + 3600000).toISOString()
  };

  beforeEach(() => {
    localStorage.clear();
    router = jasmine.createSpyObj('Router', ['navigate', 'createUrlTree']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: Router, useValue: router }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should start unauthenticated', () => {
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.currentUser()).toBeNull();
    expect(service.token()).toBeNull();
    expect(service.tenantId()).toBeNull();
  });

  describe('login', () => {
    it('should set auth response and navigate to dashboard on success', () => {
      service.login({ email: 'admin@acme.com', password: 'password' });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      expect(req.request.method).toBe('POST');
      req.flush(mockAuthResponse);

      expect(service.isAuthenticated()).toBeTrue();
      expect(service.currentUser()).toBe('Admin User');
      expect(service.token()).toBe('test-token-123');
      expect(service.tenantName()).toBe('Acme Corp');
      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });

    it('should set error on failed login', () => {
      service.login({ email: 'bad@email.com', password: 'wrong' });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush({ title: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });

      expect(service.isAuthenticated()).toBeFalse();
      expect(service.error()).toBe('Invalid credentials');
      expect(service.isLoading()).toBeFalse();
    });

    it('should set isLoading during login', () => {
      service.login({ email: 'a@b.com', password: 'p' });
      expect(service.isLoading()).toBeTrue();

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush(mockAuthResponse);
      expect(service.isLoading()).toBeFalse();
    });
  });

  describe('register', () => {
    it('should set auth and navigate to onboarding on success', () => {
      service.register({
        companyName: 'New Co',
        displayName: 'Jane',
        email: 'jane@new.co',
        password: 'password123'
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/register`);
      expect(req.request.method).toBe('POST');
      req.flush(mockAuthResponse);

      expect(service.isAuthenticated()).toBeTrue();
      expect(router.navigate).toHaveBeenCalledWith(['/onboarding']);
    });

    it('should set error on failed registration', () => {
      service.register({
        companyName: 'X',
        displayName: 'Y',
        email: 'z@z.com',
        password: 'short'
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/register`);
      req.flush({ title: 'Email already exists' }, { status: 409, statusText: 'Conflict' });

      expect(service.error()).toBe('Email already exists');
    });
  });

  describe('demoLogin', () => {
    it('should post tenant name and navigate to dashboard', () => {
      service.demoLogin('Acme Corp');

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/demo`);
      expect(req.request.body).toEqual({ tenantName: 'Acme Corp' });
      req.flush(mockAuthResponse);

      expect(service.isAuthenticated()).toBeTrue();
      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });
  });

  describe('logout', () => {
    it('should clear auth state and navigate to login', () => {
      // First login
      service.login({ email: 'a@b.com', password: 'p' });
      httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(mockAuthResponse);
      expect(service.isAuthenticated()).toBeTrue();

      // Then logout
      service.logout();

      expect(service.isAuthenticated()).toBeFalse();
      expect(service.currentUser()).toBeNull();
      expect(service.token()).toBeNull();
      expect(localStorage.getItem('tf_auth')).toBeNull();
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('localStorage hydration', () => {
    it('should hydrate from valid stored auth', () => {
      localStorage.setItem('tf_auth', JSON.stringify(mockAuthResponse));

      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        imports: [HttpClientTestingModule],
        providers: [{ provide: Router, useValue: router }]
      });

      const freshService = TestBed.inject(AuthService);
      expect(freshService.isAuthenticated()).toBeTrue();
      expect(freshService.currentUser()).toBe('Admin User');
    });

    it('should clear expired auth from storage', () => {
      const expired = { ...mockAuthResponse, expiration: new Date(Date.now() - 1000).toISOString() };
      localStorage.setItem('tf_auth', JSON.stringify(expired));

      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        imports: [HttpClientTestingModule],
        providers: [{ provide: Router, useValue: router }]
      });

      const freshService = TestBed.inject(AuthService);
      expect(freshService.isAuthenticated()).toBeFalse();
      expect(localStorage.getItem('tf_auth')).toBeNull();
    });

    it('should clear corrupted data from storage', () => {
      localStorage.setItem('tf_auth', 'not-json');

      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        imports: [HttpClientTestingModule],
        providers: [{ provide: Router, useValue: router }]
      });

      const freshService = TestBed.inject(AuthService);
      expect(freshService.isAuthenticated()).toBeFalse();
      expect(localStorage.getItem('tf_auth')).toBeNull();
    });
  });

  describe('computed signals', () => {
    it('should derive role from auth response', () => {
      service.login({ email: 'a@b.com', password: 'p' });
      httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(mockAuthResponse);

      expect(service.role()).toBe('Admin');
    });

    it('should detect expired token as unauthenticated', () => {
      const expired = { ...mockAuthResponse, expiration: new Date(Date.now() - 1000).toISOString() };
      service.login({ email: 'a@b.com', password: 'p' });
      httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(expired);

      expect(service.isAuthenticated()).toBeFalse();
    });
  });
});
