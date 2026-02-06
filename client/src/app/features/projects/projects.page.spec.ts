import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ProjectsPage } from './projects.page';
import { environment } from '../../../environments/environment';

describe('ProjectsPage', () => {
  let component: ProjectsPage;
  let fixture: ComponentFixture<ProjectsPage>;
  let httpMock: HttpTestingController;

  const mockProjects = [
    { id: '1', name: 'Project Alpha', description: 'First', status: 'Active', category: 'Dev', createdAt: new Date().toISOString() },
    { id: '2', name: 'Project Beta', description: 'Second', status: 'Planning', category: null, createdAt: new Date().toISOString() }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectsPage, HttpClientTestingModule, NoopAnimationsModule]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ProjectsPage);
    component = fixture.componentInstance;
  });

  afterEach(() => httpMock.verify());

  it('should create', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiUrl}/projects`).flush(mockProjects);
    expect(component).toBeTruthy();
  });

  it('should load projects on init', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiUrl}/projects`).flush(mockProjects);

    expect(component.projects().length).toBe(2);
    expect(component.isLoading()).toBeFalse();
  });

  it('should toggle create form visibility', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiUrl}/projects`).flush([]);

    expect(component.showCreateForm()).toBeFalse();
    component.showCreateForm.set(true);
    expect(component.showCreateForm()).toBeTrue();
  });

  it('should have required name field in create form', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiUrl}/projects`).flush([]);

    expect(component.createForm.get('name')?.hasError('required')).toBeTrue();
    component.createForm.patchValue({ name: 'New Project' });
    expect(component.createForm.valid).toBeTrue();
  });

  it('should create project and add to list', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiUrl}/projects`).flush([]);

    const newProject = { id: '3', name: 'New', description: null, status: 'Active', category: null, createdAt: new Date().toISOString() };
    component.createForm.setValue({ name: 'New', description: '', category: '' });
    component.createProject();

    const req = httpMock.expectOne(`${environment.apiUrl}/projects`);
    expect(req.request.method).toBe('POST');
    req.flush(newProject);

    expect(component.projects().length).toBe(1);
    expect(component.showCreateForm()).toBeFalse();
  });

  it('should delete project from list', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiUrl}/projects`).flush(mockProjects);

    component.deleteProject('1');

    const req = httpMock.expectOne(`${environment.apiUrl}/projects/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});

    expect(component.projects().length).toBe(1);
    expect(component.projects()[0].id).toBe('2');
  });
});
