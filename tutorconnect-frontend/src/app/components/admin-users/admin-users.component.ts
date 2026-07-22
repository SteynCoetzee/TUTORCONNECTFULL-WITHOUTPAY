import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { UserProfile, Module } from '../../models/models';
import { AuthService } from '../../services/auth.service';

interface Role { id: number; name: string; }

interface EnrollmentView {
  enrollment_ID: number;
  module_Code: string;
  module_Name: string;
  can_Book_OneOnOne: boolean;
  can_Book_Group: boolean;
  sessions_Remaining_OneOnOne: number;
  sessions_Remaining_Group: number;
  enrollment_Date: string;
}

interface PaymentView {
  payment_ID: number;
  amount: number;
  payment_Date: string;
  payment_Status: string;
  bank: string;
  payment_Reference: string;
  module_Code: string;
}

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
  deleteTargetUser: UserProfile | null = null;

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

  currentUserId: number | null = null;

  // Payments modal
  paymentUser: UserProfile | null = null;
  payments: PaymentView[] = [];
  paymentsLoading = false;

  // Student details modal
  detailStudent: UserProfile | null = null;
  detailEnrollments: EnrollmentView[] = [];
  allModules: Module[] = [];
  detailLoading = false;
  addModuleCode = '';
  addOneOnOne = false;
  addGroup = false;
  enrolling = false;
  enrollError = '';
  enrollSuccess = '';

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient, private authService: AuthService) {}

  ngOnInit() {
    this.currentUserId = this.authService.getCurrentUserId();
    this.loadUsers();
  }

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

  confirmDelete(id: number) {
    this.deleteTargetId = id;
    this.deleteTargetUser = this.users.find(u => u.user_ID === id) ?? null;
  }
  cancelDelete() { this.deleteTargetId = null; this.deleteTargetUser = null; }

  deleteUser() {
    if (!this.deleteTargetId) return;
    this.clearMessages();
    this.http.delete(`${this.apiUrl}/Users/${this.deleteTargetId}`).subscribe({
      next: () => {
        this.successMessage = 'User deleted successfully.';
        this.deleteTargetId = null;
        this.deleteTargetUser = null;
        this.loadUsers();
      },
      error: (err: any) => {
        this.errorMessage = typeof err.error === 'string' ? err.error : 'Failed to delete user.';
        this.deleteTargetId = null;
        this.deleteTargetUser = null;
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

  // ── Student Details Modal ────────────────────────────────────────────────────

  openDetails(user: UserProfile) {
    this.detailStudent = user;
    this.detailEnrollments = [];
    this.addModuleCode = '';
    this.addOneOnOne = false;
    this.addGroup = false;
    this.enrollError = '';
    this.enrollSuccess = '';
    this.detailLoading = true;

    this.http.get<EnrollmentView[]>(`${this.apiUrl}/enrollment/student/${user.user_ID}`).subscribe({
      next: (data) => {
        this.detailEnrollments = data;
        this.detailLoading = false;
      },
      error: () => { this.detailLoading = false; }
    });

    if (!this.allModules.length) {
      this.http.get<Module[]>(`${this.apiUrl}/Modules`).subscribe({
        next: (data) => { this.allModules = data; },
        error: () => {}
      });
    }
  }

  closeDetails() {
    this.detailStudent = null;
    this.enrollError = '';
    this.enrollSuccess = '';
  }

  get availableModulesToAdd(): Module[] {
    const enrolled = new Set(this.detailEnrollments.map(e => e.module_Code));
    return this.allModules.filter(m => !enrolled.has(m.module_Code));
  }

  enrollModule() {
    if (!this.detailStudent || !this.addModuleCode) {
      this.enrollError = 'Please select a module.';
      return;
    }
    if (!this.addOneOnOne && !this.addGroup) {
      this.enrollError = 'Please select at least one session type (One on One or Group).';
      return;
    }
    this.enrolling = true;
    this.enrollError = '';
    this.enrollSuccess = '';

    this.http.post<any>(`${this.apiUrl}/enrollment/enroll`, {
      student_ID: this.detailStudent.user_ID,
      module_Code: this.addModuleCode,
      can_Book_OneOnOne: this.addOneOnOne,
      can_Book_Group: this.addGroup
    }).subscribe({
      next: () => {
        this.enrolling = false;
        const mod = this.allModules.find(m => m.module_Code === this.addModuleCode);
        this.enrollSuccess = `Successfully enrolled in ${mod?.module_Name ?? this.addModuleCode}.`;
        this.addModuleCode = '';
        this.addOneOnOne = false;
        this.addGroup = false;
        // Reload enrollments
        this.http.get<EnrollmentView[]>(`${this.apiUrl}/enrollment/student/${this.detailStudent!.user_ID}`).subscribe({
          next: (data) => { this.detailEnrollments = data; },
          error: () => {}
        });
      },
      error: (err) => {
        this.enrolling = false;
        this.enrollError = typeof err.error === 'string' ? err.error : 'Failed to enroll. The student may already be enrolled.';
      }
    });
  }

  clearMessages() { this.errorMessage = ''; this.successMessage = ''; }

  // ── Payments Modal ───────────────────────────────────────────────────────────

  openPayments(user: UserProfile) {
    this.paymentUser = user;
    this.payments = [];
    this.paymentsLoading = true;
    this.http.get<PaymentView[]>(`${this.apiUrl}/Payments/student/${user.user_ID}`).subscribe({
      next: (data) => { this.payments = data; this.paymentsLoading = false; },
      error: () => { this.paymentsLoading = false; }
    });
  }

  closePayments() { this.paymentUser = null; }

  getPaymentStatusClass(status: string): string {
    if (status === 'Paid') return 'badge-success';
    if (status === 'Pending') return 'badge-orange';
    if (status === 'Failed') return 'badge-danger';
    return 'badge-teal';
  }

  get paidPayments(): PaymentView[] {
    return this.payments.filter(p => p.payment_Status === 'Paid');
  }

  get totalPaid(): number {
    return this.paidPayments.reduce((sum, p) => sum + p.amount, 0);
  }
}
