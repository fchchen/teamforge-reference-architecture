import { TestBed } from '@angular/core/testing';
import { MsalService } from './msal.service';

// Mock @azure/msal-browser with a class-based mock
vi.mock('@azure/msal-browser', () => ({
  PublicClientApplication: class MockPublicClientApplication {
    async initialize() { return undefined; }
    async loginPopup() { return { accessToken: 'mock-access-token' }; }
    async acquireTokenSilent() { return { accessToken: 'mock-silent-token' }; }
    getAllAccounts() { return [{ username: 'user@test.com' }]; }
    async logoutPopup() { return undefined; }
  }
}));

describe('MsalService', () => {
  let service: MsalService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MsalService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialize MSAL instance', async () => {
    await service.initialize();
    expect(service).toBeTruthy();
  });

  it('should return access token from loginPopup', async () => {
    await service.initialize();
    const token = await service.loginPopup();
    expect(token).toBe('mock-access-token');
  });

  it('should return access token from acquireTokenSilent', async () => {
    await service.initialize();
    const token = await service.acquireTokenSilent();
    expect(token).toBe('mock-silent-token');
  });

  it('should throw if loginPopup called before initialize', async () => {
    await expect(service.loginPopup()).rejects.toThrow('MSAL not initialized');
  });

  it('should throw if acquireTokenSilent called before initialize', async () => {
    await expect(service.acquireTokenSilent()).rejects.toThrow('MSAL not initialized');
  });

  it('should handle logout', async () => {
    await service.initialize();
    await expect(service.logout()).resolves.not.toThrow();
  });

  it('should handle logout before initialize without throwing', async () => {
    await expect(service.logout()).resolves.not.toThrow();
  });
});
