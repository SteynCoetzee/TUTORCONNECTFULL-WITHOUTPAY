import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  email = '';
  password = '';
  confirmPassword = '';
  selectedRole: 'Student' | 'Tutor' | null = null;

  loading = false;
  errorMessage = '';
  successMessage = '';
  showPassword = false;
  showConfirm = false;

  fieldErrors: Record<string, string> = {};

  constructor(private authService: AuthService, private router: Router) {}

  // ─── Live validators ────────────────────────────────────────────────────────
  private emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;

  validateEmail(value: string) {
    if (!value.trim()) { this.fieldErrors['email'] = 'Email is required.'; return; }
    if (!this.emailRegex.test(value.trim())) { this.fieldErrors['email'] = 'Enter a valid email address.'; return; }
    delete this.fieldErrors['email'];
  }

  validatePassword(value: string) {
    if (!value) { this.fieldErrors['password'] = 'Password is required.'; return; }
    if (value.length < 8) { this.fieldErrors['password'] = 'Must be at least 8 characters.'; return; }
    if (!/[A-Z]/.test(value)) { this.fieldErrors['password'] = 'Must contain at least one uppercase letter.'; return; }
    if (!/[0-9]/.test(value)) { this.fieldErrors['password'] = 'Must contain at least one number.'; return; }
    delete this.fieldErrors['password'];
    if (this.confirmPassword) this.validateConfirm(this.confirmPassword);
  }

  validateConfirm(value: string) {
    if (!value) { this.fieldErrors['confirm'] = 'Please confirm your password.'; return; }
    if (value !== this.password) { this.fieldErrors['confirm'] = 'Passwords do not match.'; return; }
    delete this.fieldErrors['confirm'];
  }

  get pwStrength(): 0 | 1 | 2 | 3 {
    if (!this.password) return 0;
    const len = this.password.length >= 8;
    const upper = /[A-Z]/.test(this.password);
    const num = /[0-9]/.test(this.password);
    const special = /[^a-zA-Z0-9]/.test(this.password);
    const score = [len, upper, num, special].filter(Boolean).length;
    if (score <= 1) return 1;
    if (score === 2 || score === 3) return 2;
    return 3;
  }

  get pwStrengthLabel(): string {
    return ['', 'Weak', 'Fair', 'Strong'][this.pwStrength];
  }

  private validateAll(): boolean {
    this.fieldErrors = {};
    this.validateEmail(this.email);
    this.validatePassword(this.password);
    this.validateConfirm(this.confirmPassword);
    return Object.keys(this.fieldErrors).length === 0;
  }

  selectRole(role: 'Student' | 'Tutor') {
    this.selectedRole = role;
    this.errorMessage = '';
    this.onRegister();
  }

  onRegister() {
    if (!this.validateAll()) {
      this.errorMessage = 'Please fix the mistakes below before continuing.';
      return;
    }
    if (!this.selectedRole) {
      this.errorMessage = 'Please select Student or Tutor to continue.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    // Student = roleId 3, Tutor registers as AW-Tutor (4) — backend handles this
    this.authService.register({
      firstName: 'New',
      lastName: 'User',
      email: this.email,
      password: this.password,
      roleId: this.selectedRole === 'Tutor' ? 2 : 3,
      phone: null,
      address: null,
      bio: null
    }).subscribe({
      next: () => {
        this.loading = false;
        this.successMessage = this.selectedRole === 'Tutor'
          ? 'Account created! You are registered as a tutor and are awaiting admin approval.'
          : 'Account created! Redirecting to login...';
        setTimeout(() => this.router.navigate(['/login']), 2000);
      },
      error: (err: any) => {
        this.loading = false;
        this.errorMessage = typeof err.error === 'string'
          ? err.error
          : 'Registration failed. The email may already be in use.';
      }
    });
  }
}
