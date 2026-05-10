import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';
import { Testimonial, TestimonialCreate, TestimonialCategory } from '../../models/models';

@Component({
  selector: 'app-admin-testimonials',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-testimonials.component.html',
  styleUrl: './admin-testimonials.component.css'
})
export class AdminTestimonialsComponent implements OnInit {
  activeTab: 'pending' | 'approved' | 'categories' | 'submit' = 'pending';

  pendingTestimonials: Testimonial[] = [];
  approvedTestimonials: Testimonial[] = [];
  categories: TestimonialCategory[] = [];

  loading = false;
  successMessage = '';
  errorMessage = '';

  // Submit form (for students)
  submitDescription = '';
  submitCategoryId: number | null = null;
  submitting = false;

  // Category form (for admins)
  showCatForm = false;
  editingCat: TestimonialCategory | null = null;
  catName = '';
  savingCat = false;
  deleteCatId: number | null = null;

  // Edit testimonial
  editingTestimonial: Testimonial | null = null;
  editDescription = '';
  editCategoryId: number | null = null;
  saving = false;

  deleteTargetId: number | null = null;

  userRole = '';
  userId = 0;
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient, private authService: AuthService) {}

  ngOnInit() {
    this.userRole = this.authService.getCurrentUserRole();
    this.userId = this.authService.getCurrentUserId() ?? 0;
    this.loadAll();
    if (this.userRole === 'Student') {
      this.activeTab = 'approved';
    }
  }

  loadAll() {
    this.loading = true;
    this.http.get<TestimonialCategory[]>(`${this.apiUrl}/Testimonials/categories`).subscribe({
      next: (cats) => {
        this.categories = cats;
        this.loadPending();
        this.loadApproved();
      },
      error: () => { this.loading = false; }
    });
  }

  loadPending() {
    this.http.get<Testimonial[]>(`${this.apiUrl}/Testimonials/pending`).subscribe({
      next: (data) => { this.pendingTestimonials = data; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  loadApproved() {
    this.http.get<Testimonial[]>(`${this.apiUrl}/Testimonials/approved`).subscribe({
      next: (data) => { this.approvedTestimonials = data; },
      error: () => {}
    });
  }

  getCategoryName(id: number): string {
    return this.categories.find(c => c.testimonial_Category_ID === id)?.test_Category_Name ?? 'Uncategorised';
  }

  // ─── APPROVE ────────────────────────────────────────────────────────────────
  approve(id: number) {
    this.clearMessages();
    this.http.put(`${this.apiUrl}/Testimonials/${id}/approve`, {}).subscribe({
      next: () => { this.successMessage = 'Testimonial approved.'; this.loadAll(); },
      error: () => { this.errorMessage = 'Failed to approve testimonial.'; }
    });
  }

  // ─── DELETE ──────────────────────────────────────────────────────────────────
  confirmDelete(id: number) { this.deleteTargetId = id; }
  cancelDelete() { this.deleteTargetId = null; }

  deleteTestimonial() {
    if (!this.deleteTargetId) return;
    this.http.delete(`${this.apiUrl}/Testimonials/${this.deleteTargetId}`).subscribe({
      next: () => {
        this.successMessage = 'Testimonial deleted.';
        this.deleteTargetId = null;
        this.loadAll();
      },
      error: () => { this.errorMessage = 'Failed to delete testimonial.'; this.deleteTargetId = null; }
    });
  }

  // ─── EDIT ────────────────────────────────────────────────────────────────────
  openEdit(t: Testimonial) {
    this.editingTestimonial = t;
    this.editDescription = t.testimonial_Description;
    this.editCategoryId = t.testimonial_Category_ID;
    this.clearMessages();
  }

  cancelEdit() {
    this.editingTestimonial = null;
  }

  saveEdit() {
    if (!this.editingTestimonial || !this.editDescription || !this.editCategoryId) {
      this.errorMessage = 'Please fill in all fields.';
      return;
    }
    this.saving = true;
    this.http.put(`${this.apiUrl}/Testimonials/${this.editingTestimonial.testimonial_ID}`, {
      description: this.editDescription,
      testimonial_Category_ID: this.editCategoryId
    }).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = 'Testimonial updated. Awaiting admin approval.';
        this.editingTestimonial = null;
        this.loadAll();
      },
      error: () => { this.saving = false; this.errorMessage = 'Failed to update testimonial.'; }
    });
  }

  // ─── SUBMIT NEW (STUDENT) ────────────────────────────────────────────────────
  submitTestimonial() {
    if (!this.submitDescription || !this.submitCategoryId) {
      this.errorMessage = 'Please fill in all fields.';
      return;
    }
    this.submitting = true;
    this.clearMessages();
    this.http.post(`${this.apiUrl}/Testimonials`, {
      description: this.submitDescription,
      student_ID: this.userId,
      testimonial_Category_ID: this.submitCategoryId
    }).subscribe({
      next: () => {
        this.submitting = false;
        this.successMessage = 'Testimonial submitted! It will appear once approved by an admin.';
        this.submitDescription = '';
        this.submitCategoryId = null;
        this.loadAll();
      },
      error: () => { this.submitting = false; this.errorMessage = 'Failed to submit testimonial.'; }
    });
  }

  // ─── CATEGORIES (ADMIN) ─────────────────────────────────────────────────────
  openCatForm(cat?: TestimonialCategory) {
    this.editingCat = cat ?? null;
    this.catName = cat?.test_Category_Name ?? '';
    this.showCatForm = true;
    this.clearMessages();
  }

  saveCat() {
    if (!this.catName) { this.errorMessage = 'Category name is required.'; return; }
    this.savingCat = true;
    const payload = { test_Category_Name: this.catName };
    const obs = this.editingCat
      ? this.http.put(`${this.apiUrl}/AdminContent/testimonial-categories/${this.editingCat.testimonial_Category_ID}`, payload)
      : this.http.post(`${this.apiUrl}/AdminContent/testimonial-categories`, payload);
    obs.subscribe({
      next: () => { this.savingCat = false; this.successMessage = 'Category saved.'; this.showCatForm = false; this.loadAll(); },
      error: () => { this.savingCat = false; this.errorMessage = 'Failed to save category.'; }
    });
  }

  confirmDeleteCat(id: number) { this.deleteCatId = id; }
  cancelDeleteCat() { this.deleteCatId = null; }

  deleteCat() {
    if (!this.deleteCatId) return;
    this.http.delete(`${this.apiUrl}/AdminContent/testimonial-categories/${this.deleteCatId}`).subscribe({
      next: () => { this.successMessage = 'Category deleted.'; this.deleteCatId = null; this.loadAll(); },
      error: () => { this.errorMessage = 'Failed to delete category.'; this.deleteCatId = null; }
    });
  }

  clearMessages() { this.errorMessage = ''; this.successMessage = ''; }
}
