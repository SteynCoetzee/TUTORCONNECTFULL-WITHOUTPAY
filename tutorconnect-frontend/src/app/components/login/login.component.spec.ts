import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth.service';

describe('LoginComponent — Login Flow & Error Handling', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authSpy: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    authSpy = jasmine.createSpyObj('AuthService', [
      'login', 'logout', 'isLoggedIn', 'getToken',
      'stopInactivityTimer', 'startInactivityTimer', 'resetInactivityTimer'
    ]);

    await TestBed.configureTestingModule({
      imports: [LoginComponent, RouterTestingModule.withRoutes([])],
      providers: [{ provide: AuthService, useValue: authSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should initialise with empty credentials, no error, and not loading', () => {
    expect(component.loginObj.email).toBe('');
    expect(component.loginObj.password).toBe('');
    expect(component.errorMessage).toBe('');
    expect(component.loading).toBeFalse();
  });

  it('should call authService.login with the entered credentials', () => {
    authSpy.login.and.returnValue(of('fake.jwt.token'));
    component.loginObj = { email: 'student@up.ac.za', password: 'Password1' };
    component.onLogin();
    expect(authSpy.login).toHaveBeenCalledWith({ email: 'student@up.ac.za', password: 'Password1' });
  });

  it('should clear a previous error message before each new login attempt', () => {
    authSpy.login.and.returnValue(of('token'));
    component.errorMessage = 'Previous error';
    component.onLogin();
    expect(component.errorMessage).toBe('');
  });

  it('should show a connection error when the API is unreachable (status 0)', () => {
    authSpy.login.and.returnValue(throwError(() => ({ status: 0 })));
    component.onLogin();
    expect(component.loading).toBeFalse();
    expect(component.errorMessage).toBe(
      'Cannot connect to the server. Please ensure the API is running.'
    );
  });

  it('should display the server error message when a string error body is returned', () => {
    authSpy.login.and.returnValue(
      throwError(() => ({ status: 401, error: 'Invalid email or password.' }))
    );
    component.onLogin();
    expect(component.errorMessage).toBe('Invalid email or password.');
  });

  it('should show a generic error message for a non-string error body', () => {
    authSpy.login.and.returnValue(
      throwError(() => ({ status: 401, error: { detail: 'unexpected' } }))
    );
    component.onLogin();
    expect(component.errorMessage).toBe('Login failed. Please check your credentials.');
  });

  it('should set loading to false after a failed login attempt', () => {
    authSpy.login.and.returnValue(throwError(() => ({ status: 500, error: 'Server error' })));
    component.onLogin();
    expect(component.loading).toBeFalse();
  });
});
