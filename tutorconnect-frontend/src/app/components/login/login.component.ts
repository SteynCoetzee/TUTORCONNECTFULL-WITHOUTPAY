import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink, CommonModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  loginObj = { email: '', password: '' };
  rememberMe = false;
  showPassword = false;
  loading = false;
  errorMessage = '';

  constructor(private authService: AuthService, private router: Router) {}

  onLogin() {
    this.errorMessage = '';
    this.loading = true;
    this.authService.login(this.loginObj).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err: any) => {
        this.loading = false;
        if (err.status === 0) {
          this.errorMessage = 'Cannot connect to the server. Please ensure the API is running.';
        } else if (typeof err.error === 'string' && err.error) {
          this.errorMessage = err.error;
        } else {
          this.errorMessage = 'Login failed. Please check your credentials.';
        }
      }
    });
  }
}
