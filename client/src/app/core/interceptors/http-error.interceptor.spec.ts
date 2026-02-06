import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { Router } from '@angular/router';
import { httpErrorInterceptor, loadingInterceptor } from './http-error.interceptor';
import { LoadingService } from '../services/loading.service';

describe('httpErrorInterceptor', () => {
  let httpMock: HttpTestingController;
  let http: HttpClient;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router }
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
    http = TestBed.inject(HttpClient);
  });

  afterEach(() => httpMock.verify());

  it('should navigate to login on 401 error', () => {
    http.get('/api/test').subscribe({ error: () => {} });

    httpMock.expectOne('/api/test')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should not navigate on non-401 errors', () => {
    http.get('/api/test').subscribe({ error: () => {} });

    httpMock.expectOne('/api/test')
      .flush(null, { status: 500, statusText: 'Server Error' });

    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should re-throw the error', () => {
    let receivedError: any;
    http.get('/api/test').subscribe({
      error: err => receivedError = err
    });

    httpMock.expectOne('/api/test')
      .flush(null, { status: 403, statusText: 'Forbidden' });

    expect(receivedError).toBeTruthy();
    expect(receivedError.status).toBe(403);
  });
});

describe('loadingInterceptor', () => {
  let httpMock: HttpTestingController;
  let http: HttpClient;
  let loadingService: LoadingService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([loadingInterceptor])),
        provideHttpClientTesting()
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
    http = TestBed.inject(HttpClient);
    loadingService = TestBed.inject(LoadingService);
  });

  afterEach(() => httpMock.verify());

  it('should show loading on request start', () => {
    http.get('/api/test').subscribe();
    expect(loadingService.isLoading()).toBeTrue();

    httpMock.expectOne('/api/test').flush({});
  });

  it('should hide loading on error', () => {
    http.get('/api/test').subscribe({ error: () => {} });

    httpMock.expectOne('/api/test')
      .flush(null, { status: 500, statusText: 'Error' });

    expect(loadingService.isLoading()).toBeFalse();
  });
});
