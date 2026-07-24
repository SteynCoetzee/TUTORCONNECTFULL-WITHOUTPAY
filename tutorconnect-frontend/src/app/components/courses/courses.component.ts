import { Component, OnInit } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { Module, Enrollment, TutorModuleAssignment } from '../../models/models';
import { environment } from '../../../environments/environment';

interface EnrollmentWithSessions extends Enrollment {
  sessions_Remaining_Group: number;
  sessions_Remaining_OneOnOne: number;
  can_Book_Group: boolean;
  can_Book_OneOnOne: boolean;
}

interface PaymentItem {
  module_Code: string;
  module_Name: string;
  sessionType: 'OneOnOne' | 'Group';
  price: number;
}

@Component({
  selector: 'app-courses',
  standalone: true,
  imports: [CommonModule, FormsModule, DecimalPipe],
  templateUrl: './courses.component.html',
  styleUrl: './courses.component.css'
})
export class CoursesComponent implements OnInit {
  modules: Module[] = [];
  loading = false;
  errorMessage = '';
  successMessage = '';

  role = '';
  userId = 0;

  // Admin
  deleteModuleCode: string | null = null;
  adminTab: 'modules' | 'wishlist' = 'modules';
  wishlistItems: { wishlist_ID: number; module_Code: string; module_Name: string; student_ID: number; date_Submitted: string; studentName: string }[] = [];
  wishlistLoading = false;
  dismissingId: number | null = null;
  confirmDismissId: number | null = null;

  // Search
  moduleSearch = '';

  // Tutor
  assignedModuleCodes = new Set<string>();
  tutorTab: 'assigned' | 'all' = 'assigned';

  get assignedModules(): Module[] {
    const q = this.moduleSearch.toLowerCase().trim();
    const assigned = this.modules.filter(m => this.assignedModuleCodes.has(m.module_Code));
    return q ? assigned.filter(m => m.module_Code.toLowerCase().includes(q) || m.module_Name.toLowerCase().includes(q)) : assigned;
  }

  get filteredAllModules(): Module[] {
    const q = this.moduleSearch.toLowerCase().trim();
    return q ? this.modules.filter(m => m.module_Code.toLowerCase().includes(q) || m.module_Name.toLowerCase().includes(q)) : this.modules;
  }

  // Student: tabs and enrollments
  studentTab: 'enrolled' | 'all' = 'enrolled';
  enrolledModuleCodes = new Set<string>();
  enrolledModules: EnrollmentWithSessions[] = [];
  selectedForEnroll = new Set<string>();
  enrollingAll = false;
  unenrollingCode = '';

  // Payment modal
  showPreConfirmModal = false;
  showPaymentModal = false;
  paymentItems: PaymentItem[] = [];
  get paymentTotal(): number {
    return this.paymentItems.reduce((sum, i) => sum + i.price, 0);
  }

  // Student: expanded modules + session-type selection
  expandedModuleCodes = new Set<string>();
  selectedSessionTypes: Record<string, Set<'OneOnOne' | 'Group'>> = {};

  get filteredEnrolledModules(): EnrollmentWithSessions[] {
    const q = this.moduleSearch.toLowerCase().trim();
    return q ? this.enrolledModules.filter(e => e.module_Code.toLowerCase().includes(q) || e.module_Name.toLowerCase().includes(q)) : this.enrolledModules;
  }

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient, private authService: AuthService, private router: Router) {}

  ngOnInit() {
    this.role = this.authService.getCurrentUserRole();
    this.userId = this.authService.getCurrentUserId() ?? 0;
    this.loadModules();

    if (this.role === 'Tutor') {
      this.loadTutorAssignments();
    } else if (this.role === 'Student') {
      this.loadEnrollments();
    } else if (this.role === 'Admin') {
      this.loadWishlist();
    }
  }

  loadModules() {
    this.loading = true;
    this.http.get<Module[]>(`${this.apiUrl}/Modules`).subscribe({
      next: (data) => { this.modules = data; this.loading = false; },
      error: () => { this.errorMessage = 'Failed to load modules.'; this.loading = false; }
    });
  }

  loadTutorAssignments() {
    this.http.get<TutorModuleAssignment[]>(`${this.apiUrl}/TutorModule/tutor/${this.userId}`).subscribe({
      next: (data) => { this.assignedModuleCodes = new Set(data.map(a => a.module_Code)); },
      error: () => {}
    });
  }

  loadEnrollments() {
    this.http.get<EnrollmentWithSessions[]>(`${this.apiUrl}/Enrollment/student/${this.userId}`).subscribe({
      next: (data) => {
        this.enrolledModules = data;
        this.enrolledModuleCodes = new Set(data.map(e => e.module_Code));
      },
      error: (err) => { this.errorMessage = 'Failed to load your enrollments. Please refresh.'; }
    });
  }

  getEnrollment(code: string): EnrollmentWithSessions | undefined {
    return this.enrolledModules.find(e => e.module_Code === code);
  }

  openModule(code: string) {
    this.router.navigate(['/dashboard/module', code]);
  }

  getModuleColor(i: number): string {
    const colors = ['#5bbfba', '#8b5cf6', '#f59e0b', '#ef4444', '#3b82f6', '#10b981'];
    return colors[i % colors.length] + '22';
  }

  // ─── Admin ───────────────────────────────────────────────────────────────────

  addNewModule() { this.router.navigate(['/dashboard/add-module']); }

  loadWishlist() {
    this.wishlistLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/ModuleWishlist`).subscribe({
      next: (data) => { this.wishlistItems = data; this.wishlistLoading = false; },
      error: () => { this.wishlistLoading = false; }
    });
  }

  dismissWishlist(id: number) {
    this.confirmDismissId = null;
    this.dismissingId = id;
    this.http.delete(`${this.apiUrl}/ModuleWishlist/${id}`).subscribe({
      next: () => {
        this.dismissingId = null;
        this.successMessage = 'Wishlist item dismissed.';
        this.loadWishlist();
      },
      error: () => { this.dismissingId = null; this.errorMessage = 'Failed to dismiss item.'; }
    });
  }

  editModule(mod: Module) {
    this.router.navigate(['/dashboard/add-module'], { queryParams: { moduleCode: mod.module_Code } });
  }

  deleteModule() {
    if (!this.deleteModuleCode) return;
    this.http.delete(`${this.apiUrl}/Modules/${this.deleteModuleCode}`).subscribe({
      next: () => {
        this.successMessage = 'Module deleted.';
        this.deleteModuleCode = null;
        this.loadModules();
      },
      error: () => { this.errorMessage = 'Failed to delete module.'; this.deleteModuleCode = null; }
    });
  }

  // ─── Student ─────────────────────────────────────────────────────────────────

  isEnrolled(code: string): boolean { return this.enrolledModuleCodes.has(code); }
  isSelected(code: string): boolean { return this.selectedForEnroll.has(code); }

  toggleSelect(code: string, event: Event) {
    event.stopPropagation();
    if (this.selectedForEnroll.has(code)) {
      this.selectedForEnroll.delete(code);
    } else {
      this.selectedForEnroll.add(code);
    }
    this.selectedForEnroll = new Set(this.selectedForEnroll);
  }

  clearSelection() {
    this.selectedForEnroll = new Set();
  }

  toggleExpand(code: string) {
    if (this.expandedModuleCodes.has(code)) {
      this.expandedModuleCodes.delete(code);
    } else {
      this.expandedModuleCodes.add(code);
    }
    this.expandedModuleCodes = new Set(this.expandedModuleCodes);
    if (!this.selectedSessionTypes[code]) {
      this.selectedSessionTypes[code] = new Set();
    }
  }

  toggleSessionType(code: string, type: 'OneOnOne' | 'Group') {
    if (!this.selectedSessionTypes[code]) this.selectedSessionTypes[code] = new Set();
    const set = this.selectedSessionTypes[code];
    if (set.has(type)) { set.delete(type); } else { set.add(type); }
    // spread to trigger Angular change detection on the record
    this.selectedSessionTypes = { ...this.selectedSessionTypes };
    if (set.size > 0) { this.selectedForEnroll.add(code); } else { this.selectedForEnroll.delete(code); }
    this.selectedForEnroll = new Set(this.selectedForEnroll);
  }

  hasSessionType(code: string, type: 'OneOnOne' | 'Group'): boolean {
    return this.selectedSessionTypes[code]?.has(type) ?? false;
  }

  switchTab(tab: 'enrolled' | 'all') {
    this.studentTab = tab;
    this.moduleSearch = '';
    this.clearSelection();
  }

  enrollSelected() {
    if (this.selectedForEnroll.size === 0) return;
    this.clearMessages();

    // Build payment summary items
    this.paymentItems = [];
    for (const code of Array.from(this.selectedForEnroll)) {
      const mod = this.modules.find(m => m.module_Code === code);
      if (!mod) continue;
      const types = this.selectedSessionTypes[code] ?? new Set();
      if (types.has('OneOnOne')) {
        this.paymentItems.push({ module_Code: code, module_Name: mod.module_Name, sessionType: 'OneOnOne', price: mod.module_Price_OneOnOne });
      }
      if (types.has('Group')) {
        this.paymentItems.push({ module_Code: code, module_Name: mod.module_Name, sessionType: 'Group', price: mod.module_Price_Group });
      }
    }

    this.showPreConfirmModal = true;
  }

  proceedToPayment() {
    this.showPreConfirmModal = false;
    this.enrollingAll = true;
    this.errorMessage = '';

    const payload = {
      studentId: this.userId,
      totalAmount: this.paymentTotal,
      items: this.paymentItems.map(i => ({
        moduleCode:  i.module_Code,
        moduleName:  i.module_Name,
        sessionType: i.sessionType,
        price:       i.price
      }))
    };

    this.http.post<{ payFastUrl: string; formData: Record<string, string> }>(
      `${this.apiUrl}/PayFast/initiate`, payload
    ).subscribe({
      next: (res) => {
        // Build a hidden POST form and submit it to PayFast
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = res.payFastUrl;
        for (const [key, value] of Object.entries(res.formData)) {
          const input = document.createElement('input');
          input.type = 'hidden';
          input.name = key;
          input.value = value;
          form.appendChild(input);
        }
        document.body.appendChild(form);
        form.submit();
      },
      error: (err) => {
        this.enrollingAll = false;
        const msg = err?.error;
        this.errorMessage = typeof msg === 'string' && msg
          ? msg
          : 'Failed to initiate payment. Please try again.';
      }
    });
  }

  cancelPreConfirm() {
    this.showPreConfirmModal = false;
  }

  confirmEnrolment() {
    this.showPaymentModal = false;
    this.enrollingAll = true;
    const codes = Array.from(this.selectedForEnroll);
    let pending = codes.length;
    let succeeded = 0;
    let alreadyEnrolled = 0;

    for (const code of codes) {
      const types = this.selectedSessionTypes[code] ?? new Set();
      this.http.post(`${this.apiUrl}/Enrollment/enroll`, {
        student_ID: this.userId,
        module_Code: code,
        can_Book_OneOnOne: types.has('OneOnOne'),
        can_Book_Group: types.has('Group')
      }).subscribe({
        next: () => { succeeded++; if (--pending === 0) this.finishEnroll(succeeded, alreadyEnrolled, codes.length); },
        error: (err) => {
          const msg: string = err?.error ?? '';
          if (msg.includes('already enrolled')) alreadyEnrolled++;
          if (--pending === 0) this.finishEnroll(succeeded, alreadyEnrolled, codes.length);
        }
      });
    }
  }

  cancelPaymentModal() {
    this.showPaymentModal = false;
  }

  private finishEnroll(succeeded: number, alreadyEnrolled: number, total: number) {
    this.enrollingAll = false;
    this.clearSelection();
    if (succeeded === total) {
      this.successMessage = `Enrolled successfully! You have 5 sessions per selected type.`;
      this.studentTab = 'enrolled';
    } else if (succeeded > 0) {
      this.successMessage = `Enrolled in ${succeeded} of ${total}. Switching to your enrolled modules.`;
      this.studentTab = 'enrolled';
    } else if (alreadyEnrolled > 0) {
      this.successMessage = 'You are already enrolled in these modules.';
      this.studentTab = 'enrolled';
    } else {
      this.errorMessage = 'Enrollment failed. Please try again or contact support.';
    }
    this.loadEnrollments();
  }

  unenroll(code: string) {
    this.unenrollingCode = code;
    this.clearMessages();
    this.http.delete(`${this.apiUrl}/Enrollment/unenroll/${code}?studentId=${this.userId}`).subscribe({
      next: () => {
        this.unenrollingCode = '';
        this.successMessage = 'Unenrolled successfully.';
        this.loadEnrollments();
      },
      error: () => { this.unenrollingCode = ''; this.errorMessage = 'Failed to unenroll.'; }
    });
  }

  clearMessages() { this.errorMessage = ''; this.successMessage = ''; }
}
