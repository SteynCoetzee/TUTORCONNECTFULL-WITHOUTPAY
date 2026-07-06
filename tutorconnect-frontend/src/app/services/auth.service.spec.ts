import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { AuthService } from './auth.service';

// Build a fake JWT whose payload contains the standard .NET claim keys
const makeToken = (expOffsetSeconds: number, role: string, userId = '7'): string => {
  const exp = Math.floor(Date.now() / 1000) + expOffsetSeconds;
  const payload = btoa(JSON.stringify({
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': userId,
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'Test User',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'test@tutorconnect.co.za',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role,
    exp
  }));
  return `eyJhbGciOiJIUzI1NiJ9.${payload}.fakesig`;
};

describe('AuthService — Authentication & Token Management', () => {
  let service: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, RouterTestingModule]
    });
    service = TestBed.inject(AuthService);
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
    service.stopInactivityTimer();
  });

  it('should store a token in localStorage and retrieve it', () => {
    service.setToken('header.payload.sig');
    expect(service.getToken()).toBe('header.payload.sig');
  });

  it('should return false for isLoggedIn when no token is stored', () => {
    expect(service.isLoggedIn()).toBeFalse();
  });

  it('should return false for isLoggedIn when the token has expired', () => {
    service.setToken(makeToken(-3600, 'Student')); // expired 1 hour ago
    expect(service.isLoggedIn()).toBeFalse();
  });

  it('should return true for isLoggedIn when the token is valid and not yet expired', () => {
    service.setToken(makeToken(3600, 'Student')); // expires in 1 hour
    expect(service.isLoggedIn()).toBeTrue();
  });

  it('should extract the correct role from the token', () => {
    service.setToken(makeToken(3600, 'Admin'));
    expect(service.getCurrentUserRole()).toBe('Admin');
  });

  it('should correctly identify an Admin user via role helpers', () => {
    service.setToken(makeToken(3600, 'Admin'));
    expect(service.isAdmin()).toBeTrue();
    expect(service.isTutor()).toBeFalse();
    expect(service.isStudent()).toBeFalse();
  });

  it('should correctly identify a Student user via role helpers', () => {
    service.setToken(makeToken(3600, 'Student'));
    expect(service.isStudent()).toBeTrue();
    expect(service.isAdmin()).toBeFalse();
    expect(service.isTutor()).toBeFalse();
  });

  it('should remove the token from localStorage on logout', () => {
    service.setToken(makeToken(3600, 'Tutor'));
    expect(service.getToken()).not.toBeNull();
    service.logout();
    expect(service.getToken()).toBeNull();
  });

  it('should return null for getCurrentUserId when no token is stored', () => {
    expect(service.getCurrentUserId()).toBeNull();
  });

  it('should extract the correct numeric user ID from the token', () => {
    service.setToken(makeToken(3600, 'Student', '42'));
    expect(service.getCurrentUserId()).toBe(42);
  });

  it('should return empty string for getCurrentUserRole when no token is stored', () => {
    expect(service.getCurrentUserRole()).toBe('');
  });
});
