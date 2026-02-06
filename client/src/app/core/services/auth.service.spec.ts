import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService, AuthResponse } from './auth.service';
import { MsalService } from './msal.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let router: { navigate: ReturnType<typeof vi.fn>; createUrlTree: ReturnType<typeof vi.fn> };
  let mockMsalService: { loginPopup: ReturnType<typeof vi.fn>; initialize: ReturnType<typeof vi.fn>; acquireTokenSilent: ReturnType<typeof vi.fn>; logout: ReturnType<typeof vi.fn> };

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
    router = { navigate: vi.fn(), createUrlTree: vi.fn() };
    mockMsalService = {
      initialize: vi.fn().mockResolvedValue(undefined),
      loginPopup: vi.fn().mockResolvedValue('mock-entra-token'),
      acquireTokenSilent: vi.fn().mockResolvedValue('mock-entra-token'),
      logout: vi.fn().mockResolvedValue(undefined)
    };

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: Router, useValue: router },
        { provide: MsalService, useValue: mockMsalService }
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
    expect(service.isAuthenticated()).toBe(false);
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

      expect(service.isAuthenticated()).toBe(true);
      expect(service.currentUser()).toBe('Admin User');
      expect(service.token()).toBe('test-token-123');
      expect(service.tenantName()).toBe('Acme Corp');
      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });

    it('should set error on failed login', () => {
      service.login({ email: 'bad@email.com', password: 'wrong' });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush({ title: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });

      expect(service.isAuthenticated()).toBe(false);
      expect(service.error()).toBe('Invalid credentials');
      expect(service.isLoading()).toBe(false);
    });

    it('should set isLoading during login', () => {
      service.login({ email: 'a@b.com', password: 'p' });
      expect(service.isLoading()).toBe(true);

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush(mockAuthResponse);
      expect(service.isLoading()).toBe(false);
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

      expect(service.isAuthenticated()).toBe(true);
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

      expect(service.isAuthenticated()).toBe(true);
      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });
  });

  describe('logout', () => {
    it('should clear auth state and navigate to login', () => {
      // First login
      service.login({ email: 'a@b.com', password: 'p' });
      httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(mockAuthResponse);
      expect(service.isAuthenticated()).toBe(true);

      // Then logout
      service.logout();

      expect(service.isAuthenticated()).toBe(false);
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
        providers: [
          { provide: Router, useValue: router },
          { provide: MsalService, useValue: mockMsalService }
        ]
      });

      const freshService = TestBed.inject(AuthService);
      expect(freshService.isAuthenticated()).toBe(true);
      expect(freshService.currentUser()).toBe('Admin User');
    });

    it('should clear expired auth from storage', () => {
      const expired = { ...mockAuthResponse, expiration: new Date(Date.now() - 1000).toISOString() };
      localStorage.setItem('tf_auth', JSON.stringify(expired));

      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        imports: [HttpClientTestingModule],
        providers: [
          { provide: Router, useValue: router },
          { provide: MsalService, useValue: mockMsalService }
        ]
      });

      const freshService = TestBed.inject(AuthService);
      expect(freshService.isAuthenticated()).toBe(false);
      expect(localStorage.getItem('tf_auth')).toBeNull();
    });

    it('should clear corrupted data from storage', () => {
      localStorage.setItem('tf_auth', 'not-json');

      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        imports: [HttpClientTestingModule],
        providers: [
          { provide: Router, useValue: router },
          { provide: MsalService, useValue: mockMsalService }
        ]
      });

      const freshService = TestBed.inject(AuthService);
      expect(freshService.isAuthenticated()).toBe(false);
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

      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('entraLogin', () => {
    it('should call MSAL loginPopup and post to entra-login endpoint', async () => {
      const entraLoginPromise = service.entraLogin();

      // Wait for MSAL popup to resolve
      await vi.waitFor(() => {
        expect(mockMsalService.loginPopup).toHaveBeenCalled();
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/entra-login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ accessToken: 'mock-entra-token' });

      req.flush({ isProvisioned: true, auth: mockAuthResponse });
      await entraLoginPromise;

      expect(service.isAuthenticated()).toBe(true);
      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });

    it('should navigate to entra-provision when not provisioned', async () => {
      const entraLoginPromise = service.entraLogin();

      await vi.waitFor(() => {
        expect(mockMsalService.loginPopup).toHaveBeenCalled();
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/entra-login`);
      req.flush({ isProvisioned: false, auth: null });
      await entraLoginPromise;

      expect(service.isAuthenticated()).toBe(false);
      expect(service.pendingEntraToken()).toBe('mock-entra-token');
      expect(router.navigate).toHaveBeenCalledWith(['/entra-provision']);
    });

    it('should set error when MSAL popup fails', async () => {
      mockMsalService.loginPopup.mockRejectedValueOnce(new Error('User cancelled'));

      await service.entraLogin();

      expect(service.error()).toBe('User cancelled');
      expect(service.isLoading()).toBe(false);
    });
  });

  describe('entraProvision', () => {
    it('should post provision request and set auth on success', async () => {
      service.pendingEntraToken.set('mock-entra-token');

      service.entraProvision('New Corp', 'New User');

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/entra-provision`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        accessToken: 'mock-entra-token',
        companyName: 'New Corp',
        displayName: 'New User'
      });

      req.flush(mockAuthResponse);

      expect(service.isAuthenticated()).toBe(true);
      expect(service.pendingEntraToken()).toBeNull();
      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });

    it('should set error when no pending token', () => {
      service.pendingEntraToken.set(null);
      service.entraProvision('Corp', 'User');

      expect(service.error()).toBe('No pending Entra ID token. Please sign in with Microsoft again.');
    });
  });
});
