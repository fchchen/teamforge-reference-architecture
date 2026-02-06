import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { TeamsPage } from './teams.page';
import { environment } from '../../../environments/environment';

describe('TeamsPage', () => {
  let component: TeamsPage;
  let fixture: ComponentFixture<TeamsPage>;
  let httpMock: HttpTestingController;

  const mockTeams = [
    { id: '1', name: 'UX Team', description: 'User experience', memberCount: 5, members: [], createdAt: new Date().toISOString() },
    { id: '2', name: 'Dev Team', description: null, memberCount: 8, members: [], createdAt: new Date().toISOString() }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TeamsPage, HttpClientTestingModule, NoopAnimationsModule]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(TeamsPage);
    component = fixture.componentInstance;
  });

  afterEach(() => httpMock.verify());

  it('should create', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiUrl}/teams`).flush(mockTeams);
    expect(component).toBeTruthy();
  });

  it('should load teams on init', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiUrl}/teams`).flush(mockTeams);

    expect(component.teams().length).toBe(2);
    expect(component.isLoading()).toBe(false);
  });

  it('should create team and add to list', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiUrl}/teams`).flush([]);

    const newTeam = { id: '3', name: 'QA Team', description: 'Quality', memberCount: 0, members: [], createdAt: new Date().toISOString() };
    component.createForm.setValue({ name: 'QA Team', description: 'Quality' });
    component.createTeam();

    const req = httpMock.expectOne(`${environment.apiUrl}/teams`);
    expect(req.request.method).toBe('POST');
    req.flush(newTeam);

    expect(component.teams().length).toBe(1);
    expect(component.showCreateForm()).toBe(false);
  });

  it('should not submit invalid form', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiUrl}/teams`).flush([]);

    component.createTeam();
    httpMock.expectNone(`${environment.apiUrl}/teams`);
  });
});
