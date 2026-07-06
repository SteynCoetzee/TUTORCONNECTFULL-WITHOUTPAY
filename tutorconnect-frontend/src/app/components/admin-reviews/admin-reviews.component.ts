import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface AdminTutorReview {
  tutor_Review_ID: number;
  tutor_Rating: number;
  student_ID: number;
  tutor_ID: number;
  tutorName: string;
  studentName: string;
}

interface AdminSessionReview {
  session_Review_ID: number;
  session_Rating: number;
  session_Description: string;
  student_ID: number;
  session_ID: number;
  slotDate: string;
  slotTime: string;
  sessionType: string;
  moduleCode: string;
  tutorName: string;
  studentName: string;
}

@Component({
  selector: 'app-admin-reviews',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-reviews.component.html',
  styleUrl: './admin-reviews.component.css'
})
export class AdminReviewsComponent implements OnInit {
  activeTab: 'tutor' | 'session' = 'tutor';
  tutorReviews: AdminTutorReview[] = [];
  sessionReviews: AdminSessionReview[] = [];
  loading = false;
  searchQuery = '';

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadTutorReviews();
    this.loadSessionReviews();
  }

  setTab(tab: 'tutor' | 'session') {
    this.activeTab = tab;
    this.searchQuery = '';
  }

  loadTutorReviews() {
    this.loading = true;
    this.http.get<AdminTutorReview[]>(`${this.apiUrl}/Reviews/admin/tutor`).subscribe({
      next: (data) => { this.tutorReviews = data; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  loadSessionReviews() {
    this.http.get<AdminSessionReview[]>(`${this.apiUrl}/Reviews/admin/session`).subscribe({
      next: (data) => { this.sessionReviews = data; },
      error: () => {}
    });
  }

  get filteredTutorReviews(): AdminTutorReview[] {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.tutorReviews;
    return this.tutorReviews.filter(r =>
      r.tutorName.toLowerCase().includes(q) || r.studentName.toLowerCase().includes(q)
    );
  }

  get filteredSessionReviews(): AdminSessionReview[] {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.sessionReviews;
    return this.sessionReviews.filter(r =>
      r.tutorName.toLowerCase().includes(q) ||
      r.studentName.toLowerCase().includes(q) ||
      (r.moduleCode && r.moduleCode.toLowerCase().includes(q))
    );
  }

  getStars(rating: number): boolean[] {
    return [1, 2, 3, 4, 5].map(n => n <= rating);
  }

  avgTutorRating(): string {
    if (!this.tutorReviews.length) return '—';
    const avg = this.tutorReviews.reduce((s, r) => s + r.tutor_Rating, 0) / this.tutorReviews.length;
    return avg.toFixed(1);
  }

  avgSessionRating(): string {
    if (!this.sessionReviews.length) return '—';
    const avg = this.sessionReviews.reduce((s, r) => s + r.session_Rating, 0) / this.sessionReviews.length;
    return avg.toFixed(1);
  }

  formatTime(t: string): string {
    try {
      const [h, m] = t.split(':').map(Number);
      const d = new Date(); d.setHours(h, m);
      return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
    } catch { return t; }
  }

  formatDate(d: string): string {
    try { return new Date(d).toLocaleDateString('en-ZA', { year: 'numeric', month: 'short', day: 'numeric' }); }
    catch { return d; }
  }
}
