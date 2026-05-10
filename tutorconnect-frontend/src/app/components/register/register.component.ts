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
  firstName = '';
  lastName = '';
  email = '';
  password = '';
  phone = '';
  address = '';
  bio = '';

  selectedRole: 'Student' | 'Tutor' = 'Student';

  loading = false;
  errorMessage = '';
  successMessage = '';
  showPassword = false;

  constructor(private authService: AuthService, private router: Router) {}

  onRegister() {
    if (!this.firstName || !this.lastName || !this.email || !this.password) {
      this.errorMessage = 'Please fill in all required fields.';
      return;
    }
    if (this.password.length < 6) {
      this.errorMessage = 'Password must be at least 6 characters.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.register({
      firstName: this.firstName,
      lastName: this.lastName,
      email: this.email,
      password: this.password,
      roleId: 3, // All registrations start as Student; admin promotes Tutors
      phone: this.phone || null,
      address: this.address || null,
      bio: this.bio || null
    }).subscribe({
      next: () => {
        this.loading = false;
        if (this.selectedRole === 'Tutor') {
          this.successMessage = 'Account created! Your tutor access is pending admin approval. You can log in now with student access.';
        } else {
          this.successMessage = 'Account created successfully! Redirecting to login...';
        }
        setTimeout(() => this.router.navigate(['/login']), 2500);
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
