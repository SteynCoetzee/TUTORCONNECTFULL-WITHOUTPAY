import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

interface TutorReview {
  tutor_Review_ID: number;
  tutor_Rating: number;
  student_ID: number;
  tutor_ID: number;
  tutorName: string;
  studentName?: string;
}

interface SessionReview {
  session_Review_ID: number;
  session_Rating: number;
  session_Description: string;
  student_ID: number;
  session_ID: number;
  slotDate: string;
  slotTime: string;
  sessionType: string;
  tutorName: string;
  studentName?: string;
  moduleCode?: string;
}

interface TutorOption {
  tutorId: number;
  tutorName: string;
  modules: { moduleCode: string; moduleName: string }[];
}

interface SessionOption {
  booking_ID: number;
  booking_Slot_ID: number;
  slotDate: string;
  slotTime: string;
  sessionType: string;
  tutorName: string;
}

@Component({
  selector: 'app-reviews',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reviews.component.html',
  styleUrl: './reviews.component.css'
})
export class ReviewsComponent implements OnInit {
  activeTab: 'tutor' | 'session' = 'tutor';
  tutorReviews: TutorReview[] = [];
  sessionReviews: SessionReview[] = [];
  loading = false;
  errorMessage = '';
  successMessage = '';

  // Inline tutor review form
  showTutorForm = false;
  selectedTutorId: number | null = null;
  tutorRating = 5;
  savingTutor = false;
  tutorOptions: TutorOption[] = [];

  // Inline session review form
  showSessionForm = false;
  selectedBookingSlotId: number | null = null;
  sessionRating = 5;
  sessionDescription = '';
  savingSession = false;
  sessionOptions: SessionOption[] = [];

  // Inline delete confirm
  confirmDeleteTutorId: number | null = null;
  confirmDeleteSessionId: number | null = null;

  private apiUrl = environment.apiUrl;
  userId = 0;
  userRole = '';

  constructor(private http: HttpClient, private authService: AuthService) {}

  ngOnInit() {
    this.userId = this.authService.getCurrentUserId() ?? 0;
    this.userRole = this.authService.getCurrentUserRole();
    this.loadTutorReviews();
    if (!this.isTutor) {
      this.loadTutorOptions();
      this.loadSessionOptions();
    }
    // Tutors start on the tutor tab; preload their session reviews in background
    if (this.isTutor) {
      this.loadSessionReviews();
    }
  }

  get isTutor(): boolean { return this.userRole === 'Tutor'; }

  setTab(tab: 'tutor' | 'session') {
    this.activeTab = tab;
    this.showTutorForm = false;
    this.showSessionForm = false;
    this.confirmDeleteTutorId = null;
    this.confirmDeleteSessionId = null;
    this.clearMessages();
    if (tab === 'tutor') this.loadTutorReviews();
    else this.loadSessionReviews();
  }

  loadTutorReviews() {
    this.loading = true;
    // Tutors see reviews written FOR them; students see reviews they wrote
    const url = this.isTutor
      ? `${this.apiUrl}/Reviews/tutor/${this.userId}`
      : `${this.apiUrl}/Reviews/tutor/student/${this.userId}`;
    this.http.get<TutorReview[]>(url).subscribe({
      next: (data) => { this.tutorReviews = data; this.loading = false; },
      error: () => { this.tutorReviews = []; this.loading = false; }
    });
  }

  loadSessionReviews() {
    this.loading = true;
    const url = this.isTutor
      ? `${this.apiUrl}/Reviews/session/tutor/${this.userId}`
      : `${this.apiUrl}/Reviews/session/student/${this.userId}`;
    this.http.get<any[]>(url).subscribe({
      next: (data) => {
        this.sessionReviews = data.map(r => ({
          session_Review_ID: r.session_Review_ID,
          session_Rating:    r.session_Rating,
          session_Description: r.session_Description ?? '',
          student_ID:        r.student_ID,
          session_ID:        r.session_ID,
          slotDate:          r.slotDate ?? '',
          slotTime:          r.slotTime ?? '',
          sessionType:       r.sessionType ?? '',
          tutorName:         r.tutorName ?? '',
          studentName:       r.studentName ?? '',
          moduleCode:        r.moduleCode ?? ''
        }));
        this.loading = false;
      },
      error: () => { this.sessionReviews = []; this.loading = false; }
    });
  }

  loadTutorOptions() {
    this.http.get<TutorOption[]>(`${this.apiUrl}/Reviews/tutors-for-student/${this.userId}`).subscribe({
      next: (data) => { this.tutorOptions = data; },
      error: () => { this.tutorOptions = []; }
    });
  }

  loadSessionOptions() {
    this.http.get<SessionOption[]>(`${this.apiUrl}/Bookings/student/${this.userId}`).subscribe({
      next: (data) => { this.sessionOptions = data; },
      error: () => { this.sessionOptions = []; }
    });
  }

  openTutorForm() {
    this.showTutorForm = true;
    this.selectedTutorId = null;
    this.tutorRating = 5;
    this.clearMessages();
  }

  cancelTutorForm() { this.showTutorForm = false; this.clearMessages(); }

  openSessionForm() {
    this.showSessionForm = true;
    this.selectedBookingSlotId = null;
    this.sessionRating = 5;
    this.sessionDescription = '';
    this.clearMessages();
  }

  cancelSessionForm() { this.showSessionForm = false; this.clearMessages(); }

  submitTutorReview() {
    if (!this.selectedTutorId) { this.errorMessage = 'Please select a tutor.'; return; }
    this.savingTutor = true;
    this.clearMessages();
    this.http.post(`${this.apiUrl}/Reviews/tutor`, {
      rating: this.tutorRating,
      student_ID: this.userId,
      tutor_ID: this.selectedTutorId
    }).subscribe({
      next: () => {
        this.savingTutor = false;
        this.successMessage = 'Tutor review submitted!';
        this.showTutorForm = false;
        this.loadTutorReviews();
      },
      error: () => { this.savingTutor = false; this.errorMessage = 'Failed to submit review.'; }
    });
  }

  submitSessionReview() {
    if (!this.selectedBookingSlotId || !this.sessionDescription) {
      this.errorMessage = 'Please select a session and add a description.'; return;
    }
    this.savingSession = true;
    this.clearMessages();
    this.http.post(`${this.apiUrl}/Reviews/session`, {
      rating: this.sessionRating,
      description: this.sessionDescription,
      student_ID: this.userId,
      session_ID: this.selectedBookingSlotId
    }).subscribe({
      next: () => {
        this.savingSession = false;
        this.successMessage = 'Session review submitted!';
        this.showSessionForm = false;
        this.loadSessionReviews();
      },
      error: () => { this.savingSession = false; this.errorMessage = 'Failed to submit review.'; }
    });
  }

  // ── Inline delete confirm ────────────────────────────────────────────────────
  requestDeleteTutor(id: number)   { this.confirmDeleteTutorId = id;   this.confirmDeleteSessionId = null; }
  requestDeleteSession(id: number) { this.confirmDeleteSessionId = id; this.confirmDeleteTutorId = null; }
  cancelDelete() { this.confirmDeleteTutorId = null; this.confirmDeleteSessionId = null; }

  confirmDeleteTutor() {
    if (!this.confirmDeleteTutorId) return;
    this.http.delete(`${this.apiUrl}/Reviews/tutor/${this.confirmDeleteTutorId}`).subscribe({
      next: () => { this.confirmDeleteTutorId = null; this.successMessage = 'Review deleted.'; this.loadTutorReviews(); },
      error: () => { this.errorMessage = 'Failed to delete review.'; }
    });
  }

  confirmDeleteSession() {
    if (!this.confirmDeleteSessionId) return;
    this.http.delete(`${this.apiUrl}/Reviews/session/${this.confirmDeleteSessionId}`).subscribe({
      next: () => { this.confirmDeleteSessionId = null; this.successMessage = 'Review deleted.'; this.loadSessionReviews(); },
      error: () => { this.errorMessage = 'Failed to delete review.'; }
    });
  }

  getTutorModuleLabel(t: TutorOption): string {
    return t.modules.map(m => m.moduleCode).join(', ');
  }

  formatSessionLabel(s: SessionOption): string {
    return `${s.slotDate} ${this.formatTime(s.slotTime)} · ${s.sessionType} · ${s.tutorName}`;
  }

  formatTime(timeStr: string): string {
    try {
      const [h, m] = timeStr.split(':').map(Number);
      const d = new Date(); d.setHours(h, m);
      return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
    } catch { return timeStr; }
  }

  setTutorRating(star: number)   { this.tutorRating = star; }
  setSessionRating(star: number) { this.sessionRating = star; }

  getStars(): number[] { return [1, 2, 3, 4, 5]; }
  clearMessages() { this.errorMessage = ''; this.successMessage = ''; }
}
