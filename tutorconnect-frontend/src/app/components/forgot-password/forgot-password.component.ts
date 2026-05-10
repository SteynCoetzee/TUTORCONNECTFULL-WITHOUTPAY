import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css'
})
export class ForgotPasswordComponent {
  step: 'email' | 'code' | 'password' = 'email';
  email = '';
  resetCode = '';
  newPassword = '';
  confirmPassword = '';
  loading = false;
  errorMessage = '';
  successMessage = '';
  showPassword = false;
  showConfirmPassword = false;
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient, private router: Router) {}

  requestReset() {
    if (!this.email) {
      this.errorMessage = 'Please enter your email address.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post(`${this.apiUrl}/Auth/forgot-password`, { email: this.email }).subscribe({
      next: (response: any) => {
        this.loading = false;
        this.successMessage = response.message || 'Reset code sent to your email.';
        // For development: show the reset code
        if (response.resetCode) {
          alert(`Development mode: Your reset code is ${response.resetCode}`);
        }
        this.step = 'code';
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error || 'Failed to process request.';
      }
    });
  }

  verifyCode() {
    if (!this.resetCode) {
      this.errorMessage = 'Please enter the reset code.';
      return;
    }

    this.step = 'password';
    this.errorMessage = '';
  }

  resetPassword() {
    if (!this.newPassword || !this.confirmPassword) {
      this.errorMessage = 'Please fill in all password fields.';
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    if (this.newPassword.length < 6) {
      this.errorMessage = 'Password must be at least 6 characters long.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.http.post(`${this.apiUrl}/Auth/reset-password`, {
      email: this.email,
      resetCode: this.resetCode,
      newPassword: this.newPassword
    }).subscribe({
      next: () => {
        this.loading = false;
        this.successMessage = 'Password successfully reset! Redirecting to login...';
        setTimeout(() => this.router.navigate(['/login']), 2000);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error || 'Failed to reset password.';
      }
    });
  }

  goBack() {
    if (this.step === 'code') {
      this.step = 'email';
      this.resetCode = '';
    } else if (this.step === 'password') {
      this.step = 'code';
      this.newPassword = '';
      this.confirmPassword = '';
    }
    this.errorMessage = '';
  }
}
