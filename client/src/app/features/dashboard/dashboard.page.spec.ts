import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { DashboardPage } from './dashboard.page';
import { environment } from '../../../environments/environment';

describe('DashboardPage', () => {
  let component: DashboardPage;
  let fixture: ComponentFixture<DashboardPage>;
  let httpMock: HttpTestingController;

  const mockDashboard = {
    projectCount: 5,
    teamCount: 3,
    userCount: 12,
    recentProjects: [
      { id: '1', name: 'Project Alpha', description: 'First project', status: 'Active', category: 'Dev', createdAt: new Date().toISOString() }
    ],
    recentAnnouncements: [
      { id: '1', title: 'Welcome', content: 'Hello team', createdByName: 'Admin', createdAt: new Date().toISOString() }
    ]
  };

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [DashboardPage, HttpClientTestingModule, RouterTestingModule, NoopAnimationsModule]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(DashboardPage);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should create', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiUrl}/dashboard`).flush(mockDashboard);
    expect(component).toBeTruthy();
  });

  it('should show loading spinner initially', () => {
    fixture.detectChanges();
    expect(component.isLoading()).toBeTrue();

    const spinner = fixture.nativeElement.querySelector('mat-spinner');
    expect(spinner).toBeTruthy();

    httpMock.expectOne(`${environment.apiUrl}/dashboard`).flush(mockDashboard);
  });

  it('should display stats after loading', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiUrl}/dashboard`).flush(mockDashboard);
    fixture.detectChanges();

    expect(component.isLoading()).toBeFalse();
    expect(component.data()?.projectCount).toBe(5);
    expect(component.data()?.teamCount).toBe(3);
    expect(component.data()?.userCount).toBe(12);
  });

  it('should handle API error gracefully', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiUrl}/dashboard`)
      .flush(null, { status: 500, statusText: 'Error' });
    fixture.detectChanges();

    expect(component.isLoading()).toBeFalse();
    expect(component.data()).toBeNull();
  });
});
