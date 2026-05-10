import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { UserProfile } from '../../models/models';

interface Role { id: number; name: string; }

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css'
})
export class AdminUsersComponent implements OnInit {
  activeTab: 'users' | 'pending' = 'users';

  users: UserProfile[] = [];
  filteredUsers: UserProfile[] = [];
  loading = false;
  errorMessage = '';
  successMessage = '';

  filterRole = 'All';
  searchQuery = '';
  deleteTargetId: number | null = null;

  approvingId: number | null = null;

  // Edit user
  editingUser: UserProfile | null = null;
  editFirstName = '';
  editLastName = '';
  editPhone = '';
  editAddress = '';
  editBio = '';
  editRoleId: number | null = null;
  saving = false;

  roles: Role[] = [
    { id: 1, name: 'Admin' },
    { id: 2, name: 'Tutor' },
    { id: 3, name: 'Student' },
    { id: 4, name: 'AW-Tutor' },
  ];

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnInit() { this.loadUsers(); }

  loadUsers() {
    this.loading = true;
    this.http.get<UserProfile[]>(`${this.apiUrl}/Users`).subscribe({
      next: (data) => { this.users = data; this.applyFilters(); this.loading = false; },
      error: () => { this.errorMessage = 'Failed to load users.'; this.loading = false; }
    });
  }

  applyFilters() {
    let result = this.users;
    if (this.filterRole !== 'All') {
      result = result.filter(u => u.roleName === this.filterRole);
    }
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(u =>
        u.firstName.toLowerCase().includes(q) ||
        u.lastName.toLowerCase().includes(q) ||
        u.email.toLowerCase().includes(q)
      );
    }
    this.filteredUsers = result;
  }

  openEdit(user: UserProfile) {
    this.editingUser = user;
    this.editFirstName = user.firstName;
    this.editLastName = user.lastName;
    this.editPhone = user.phone ?? '';
    this.editAddress = user.address ?? '';
    this.editBio = user.bio ?? '';
    this.editRoleId = user.user_Role_ID;
    this.clearMessages();
  }

  closeEdit() {
    this.editingUser = null;
  }

  saveEdit() {
    if (!this.editingUser || !this.editFirstName.trim() || !this.editLastName.trim() || !this.editRoleId) {
      this.errorMessage = 'First name, last name and role are required.';
      return;
    }
    this.saving = true;
    this.clearMessages();
    this.http.put(`${this.apiUrl}/Users/${this.editingUser.user_ID}/admin`, {
      firstName: this.editFirstName.trim(),
      lastName: this.editLastName.trim(),
      phone: this.editPhone || null,
      address: this.editAddress || null,
      bio: this.editBio || null,
      roleId: this.editRoleId
    }).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = `${this.editFirstName}'s profile updated successfully.`;
        this.closeEdit();
        this.loadUsers();
      },
      error: () => { this.saving = false; this.errorMessage = 'Failed to update user.'; }
    });
  }

  confirmDelete(id: number) { this.deleteTargetId = id; }
  cancelDelete() { this.deleteTargetId = null; }

  deleteUser() {
    if (!this.deleteTargetId) return;
    this.clearMessages();
    this.http.delete(`${this.apiUrl}/Users/${this.deleteTargetId}`).subscribe({
      next: () => {
        this.successMessage = 'User deleted successfully.';
        this.deleteTargetId = null;
        this.loadUsers();
      },
      error: (err: any) => {
        this.errorMessage = typeof err.error === 'string' ? err.error : 'Failed to delete user.';
        this.deleteTargetId = null;
      }
    });
  }

  get awTutors() { return this.users.filter(u => u.roleName === 'AW-Tutor'); }

  approveUser(user: UserProfile) {
    this.approvingId = user.user_ID;
    this.clearMessages();
    this.http.put(`${this.apiUrl}/Users/${user.user_ID}/admin`, {
      firstName: user.firstName,
      lastName: user.lastName,
      phone: user.phone || null,
      address: user.address || null,
      bio: user.bio || null,
      roleId: 2
    }).subscribe({
      next: () => {
        this.approvingId = null;
        this.successMessage = `${user.firstName} approved as Tutor.`;
        this.loadUsers();
      },
      error: () => { this.approvingId = null; this.errorMessage = 'Failed to approve tutor.'; }
    });
  }

  get studentCount() { return this.users.filter(u => u.roleName === 'Student').length; }
  get tutorCount() { return this.users.filter(u => u.roleName === 'Tutor').length; }
  get adminCount() { return this.users.filter(u => u.roleName === 'Admin').length; }
  get awTutorCount() { return this.awTutors.length; }

  getRoleBadgeClass(role?: string): string {
    if (role === 'Admin') return 'badge-purple';
    if (role === 'Tutor') return 'badge-teal';
    if (role === 'AW-Tutor') return 'badge-orange';
    return 'badge-success';
  }

  getInitials(user: UserProfile): string {
    return `${user.firstName?.charAt(0) ?? ''}${user.lastName?.charAt(0) ?? ''}`.toUpperCase();
  }

  clearMessages() { this.errorMessage = ''; this.successMessage = ''; }
}
