import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { UserProfile, UserProfileUpdate } from '../../models/models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-user-info',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-info.component.html',
  styleUrl: './user-info.component.css'
})
export class UserInfoComponent implements OnInit {
  profile: UserProfile | null = null;
  editMode = false;
  saving = false;
  successMessage = '';
  errorMessage = '';

  editData: UserProfileUpdate = { firstName: '', lastName: '' };

  // Role-specific profile
  roleProfile: any = null;
  editRoleProfile: any = {};
  savingRole = false;

  // Change password
  showPasswordForm = false;
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  savingPassword = false;
  passwordError = '';
  passwordSuccess = '';

  private apiUrl = environment.apiUrl;

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private http: HttpClient
  ) {}

  ngOnInit() {
    const userId = this.authService.getCurrentUserId();
    if (userId) {
      this.userService.getUser(userId).subscribe({
        next: (data) => {
          this.profile = data;
          this.loadRoleProfile(userId);
        },
        error: () => { this.errorMessage = 'Failed to load profile.'; }
      });
    }
  }

  loadRoleProfile(userId: number) {
    this.http.get<any>(`${this.apiUrl}/Users/${userId}/role-profile`).subscribe({
      next: (data) => {
        this.roleProfile = data;
        this.editRoleProfile = data ? { ...data } : {};
      },
      error: () => {}
    });
  }

  startEdit() {
    if (!this.profile) return;
    this.editData = {
      firstName: this.profile.firstName,
      lastName: this.profile.lastName,
      phone: this.profile.phone,
      address: this.profile.address,
      bio: this.profile.bio
    };
    this.editRoleProfile = this.roleProfile ? { ...this.roleProfile } : {};
    this.editMode = true;
  }

  cancelEdit() {
    this.editMode = false;
    this.errorMessage = '';
  }

  saveProfile() {
    const userId = this.authService.getCurrentUserId();
    if (!userId) return;
    this.saving = true;
    this.userService.updateUser(userId, this.editData).subscribe({
      next: () => {
        this.saving = false;
        this.editMode = false;
        this.successMessage = 'Profile updated successfully!';
        this.userService.getUser(userId).subscribe({ next: (d) => { this.profile = d; } });
        this.saveRoleProfile(userId);
        setTimeout(() => { this.successMessage = ''; }, 3000);
      },
      error: () => { this.saving = false; this.errorMessage = 'Failed to save profile.'; }
    });
  }

  saveRoleProfile(userId: number) {
    const role = this.profile?.roleName;
    let endpoint = '';
    if (role === 'Student') endpoint = `${this.apiUrl}/Users/${userId}/student-profile`;
    else if (role === 'Tutor' || role === 'AW-Tutor') endpoint = `${this.apiUrl}/Users/${userId}/tutor-profile`;
    else if (role === 'Admin') endpoint = `${this.apiUrl}/Users/${userId}/admin-profile`;
    if (!endpoint) return;
    this.http.put(endpoint, this.editRoleProfile, { responseType: 'text' }).subscribe({
      next: () => { this.roleProfile = { ...this.editRoleProfile }; },
      error: () => {}
    });
  }

  openPasswordForm() {
    this.showPasswordForm = true;
    this.currentPassword = '';
    this.newPassword = '';
    this.confirmPassword = '';
    this.passwordError = '';
    this.passwordSuccess = '';
  }

  cancelPasswordForm() {
    this.showPasswordForm = false;
    this.passwordError = '';
    this.passwordSuccess = '';
  }

  changePassword() {
    if (!this.currentPassword || !this.newPassword || !this.confirmPassword) {
      this.passwordError = 'Please fill in all password fields.';
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.passwordError = 'New passwords do not match.';
      return;
    }
    if (this.newPassword.length < 6) {
      this.passwordError = 'New password must be at least 6 characters.';
      return;
    }
    const userId = this.authService.getCurrentUserId();
    if (!userId) return;
    this.savingPassword = true;
    this.passwordError = '';
    this.http.put(`${this.apiUrl}/Auth/change-password/${userId}`, {
      currentPassword: this.currentPassword,
      newPassword: this.newPassword,
      confirmPassword: this.confirmPassword
    }, { responseType: 'text' }).subscribe({
      next: () => {
        this.savingPassword = false;
        this.passwordSuccess = 'Password updated successfully!';
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        setTimeout(() => { this.showPasswordForm = false; this.passwordSuccess = ''; }, 2000);
      },
      error: (err) => {
        this.savingPassword = false;
        this.passwordError = err.error || 'Failed to update password.';
      }
    });
  }

  get roleName(): string { return this.profile?.roleName ?? ''; }

  getStudentId(): string {
    if (!this.profile) return '';
    return `STU${new Date().getFullYear()}${String(this.profile.user_ID).padStart(3, '0')}`;
  }
}
