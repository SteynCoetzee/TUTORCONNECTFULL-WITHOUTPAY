import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { Module, Enrollment, TutorModuleAssignment } from '../../models/models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-courses',
  standalone: true,
  imports: [CommonModule, FormsModule],
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

  // Tutor
  assignedModuleCodes = new Set<string>();
  tutorTab: 'assigned' | 'all' = 'assigned';

  get assignedModules(): Module[] {
    return this.modules.filter(m => this.assignedModuleCodes.has(m.module_Code));
  }

  // Student: tabs and enrollments
  studentTab: 'enrolled' | 'all' = 'enrolled';
  enrolledModuleCodes = new Set<string>();
  selectedForEnroll = new Set<string>();
  enrollingAll = false;
  unenrollingCode = '';

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
    this.http.get<Enrollment[]>(`${this.apiUrl}/Enrollment/student/${this.userId}`).subscribe({
      next: (data) => { this.enrolledModuleCodes = new Set(data.map(e => e.module_Code)); },
      error: () => {}
    });
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

  get enrolledModules(): Module[] {
    return this.modules.filter(m => this.enrolledModuleCodes.has(m.module_Code));
  }

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

  switchTab(tab: 'enrolled' | 'all') {
    this.studentTab = tab;
    this.clearSelection();
  }

  enrollSelected() {
    if (this.selectedForEnroll.size === 0) return;
    this.enrollingAll = true;
    this.clearMessages();
    const codes = Array.from(this.selectedForEnroll);
    let pending = codes.length;
    let succeeded = 0;

    for (const code of codes) {
      this.http.post(`${this.apiUrl}/Enrollment/enroll`, {
        student_ID: this.userId,
        module_Code: code
      }).subscribe({
        next: () => { succeeded++; if (--pending === 0) this.finishEnroll(succeeded, codes.length); },
        error: () => { if (--pending === 0) this.finishEnroll(succeeded, codes.length); }
      });
    }
  }

  private finishEnroll(succeeded: number, total: number) {
    this.enrollingAll = false;
    this.clearSelection();
    if (succeeded === total) {
      this.successMessage = `Enrolled in ${succeeded} module${succeeded > 1 ? 's' : ''}!`;
    } else if (succeeded > 0) {
      this.successMessage = `Enrolled in ${succeeded} of ${total} modules. Some may already be enrolled.`;
    } else {
      this.errorMessage = 'Enrollment failed. You may already be enrolled in these modules.';
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
