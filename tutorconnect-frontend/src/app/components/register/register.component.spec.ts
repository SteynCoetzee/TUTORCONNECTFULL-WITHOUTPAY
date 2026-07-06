import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { RegisterComponent } from './register.component';
import { AuthService } from '../../services/auth.service';

describe('RegisterComponent — Registration Input Validation', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;

  beforeEach(async () => {
    const authSpy = jasmine.createSpyObj('AuthService', ['register']);

    await TestBed.configureTestingModule({
      imports: [RegisterComponent, RouterTestingModule.withRoutes([])],
      providers: [{ provide: AuthService, useValue: authSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── Email validation ─────────────────────────────────────────────────────────

  it('should set an error when the email field is empty', () => {
    component.validateEmail('');
    expect(component.fieldErrors['email']).toBe('Email is required.');
  });

  it('should set an error for a malformed email address', () => {
    component.validateEmail('not-an-email');
    expect(component.fieldErrors['email']).toBe('Enter a valid email address.');
  });

  it('should accept a well-formed email address and clear the error', () => {
    component.validateEmail('student@up.ac.za');
    expect(component.fieldErrors['email']).toBeUndefined();
  });

  // ── Password validation ───────────────────────────────────────────────────────

  it('should reject a password shorter than 8 characters', () => {
    component.validatePassword('Ab1');
    expect(component.fieldErrors['password']).toBe('Must be at least 8 characters.');
  });

  it('should reject a password that has no uppercase letter', () => {
    component.validatePassword('password1');
    expect(component.fieldErrors['password']).toBe('Must contain at least one uppercase letter.');
  });

  it('should reject a password that has no numeric digit', () => {
    component.validatePassword('PasswordOnly');
    expect(component.fieldErrors['password']).toBe('Must contain at least one number.');
  });

  it('should accept a password that satisfies all requirements', () => {
    component.validatePassword('Password1');
    expect(component.fieldErrors['password']).toBeUndefined();
  });

  // ── Confirm-password validation ───────────────────────────────────────────────

  it('should set an error when the confirm field is empty', () => {
    component.password = 'Password1';
    component.validateConfirm('');
    expect(component.fieldErrors['confirm']).toBe('Please confirm your password.');
  });

  it('should set an error when the confirm password does not match', () => {
    component.password = 'Password1';
    component.validateConfirm('Password2');
    expect(component.fieldErrors['confirm']).toBe('Passwords do not match.');
  });

  it('should clear the confirm error when the passwords match', () => {
    component.password = 'Password1';
    component.validateConfirm('Password1');
    expect(component.fieldErrors['confirm']).toBeUndefined();
  });

  // ── Password strength meter ───────────────────────────────────────────────────

  it('should return strength 0 for an empty password', () => {
    component.password = '';
    expect(component.pwStrength).toBe(0);
    expect(component.pwStrengthLabel).toBe('');
  });

  it('should return strength 1 (Weak) for a password meeting only one criterion', () => {
    component.password = 'abc'; // no length, no upper, no digit, no special
    expect(component.pwStrength).toBe(1);
    expect(component.pwStrengthLabel).toBe('Weak');
  });

  it('should return strength 2 (Fair) for a password meeting three criteria', () => {
    component.password = 'Password1'; // length + upper + digit = 3 criteria
    expect(component.pwStrength).toBe(2);
    expect(component.pwStrengthLabel).toBe('Fair');
  });

  it('should return strength 3 (Strong) for a password meeting all four criteria', () => {
    component.password = 'Password1!'; // length + upper + digit + special
    expect(component.pwStrength).toBe(3);
    expect(component.pwStrengthLabel).toBe('Strong');
  });
});
