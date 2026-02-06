import { TestBed } from '@angular/core/testing';
import { LoadingService } from './loading.service';

describe('LoadingService', () => {
  let service: LoadingService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LoadingService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should start with isLoading false', () => {
    expect(service.isLoading()).toBe(false);
  });

  it('should set isLoading to true on show()', () => {
    service.show();
    expect(service.isLoading()).toBe(true);
  });

  it('should set isLoading to false on hide()', () => {
    service.show();
    service.hide();
    expect(service.isLoading()).toBe(false);
  });
});
