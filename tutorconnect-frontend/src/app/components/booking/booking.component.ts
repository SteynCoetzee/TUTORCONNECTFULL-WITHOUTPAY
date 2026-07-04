import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

interface EnrolledModule {
  module_Code: string;
  module_Name: string;
  can_Book_OneOnOne: boolean;
  can_Book_Group: boolean;
  sessions_Remaining_Group: number;
  sessions_Remaining_OneOnOne: number;
}

interface AvailableSlot {
  booking_Slot_ID: number;
  slotDate: string;
  slotTime: string;
  session_Type: string;
  location: string;
  tutor_ID: number;
  tutorName: string;
  maxCapacity: number | null;
  bookingCount: number;
  remaining: number;
}

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './booking.component.html',
  styleUrl: './booking.component.css'
})
export class BookingComponent implements OnInit {
  activeTab: 'book' | 'my-bookings' = 'book';

  // My Bookings
  myBookings: any[] = [];
  loadingBookings = false;
  cancellingId: number | null = null;
  confirmCancelId: number | null = null;

  enrolledModules: EnrolledModule[] = [];
  allSlots: AvailableSlot[] = [];
  filteredSlots: AvailableSlot[] = [];

  selectedModule = '';
  selectedFormat = '';   // 'One-on-One' | 'Group'
  selectedSlotId: number | null = null;

  loadingModules = false;
  loadingSlots = false;
  booking = false;
  successMessage = '';
  errorMessage = '';

  // Session-out modal
  showSessionOutModal = false;
  sessionOutType = '';   // 'Group' or 'OneOnOne'
  sessionOutModule = '';
  unenrolling = false;

  private apiUrl = environment.apiUrl;
  private userId = 0;

  constructor(private http: HttpClient, private authService: AuthService) {}

  ngOnInit() {
    this.userId = this.authService.getCurrentUserId() ?? 0;
    this.loadEnrolledModules();
    this.loadMyBookings();
  }

  loadMyBookings() {
    this.loadingBookings = true;
    this.http.get<any[]>(`${this.apiUrl}/Bookings/student/${this.userId}`).subscribe({
      next: (data) => { this.myBookings = data; this.loadingBookings = false; },
      error: () => { this.loadingBookings = false; }
    });
  }

  confirmCancel(bookingId: number) { this.confirmCancelId = bookingId; }
  dismissCancel() { this.confirmCancelId = null; }

  cancelBooking(bookingId: number) {
    this.cancellingId = bookingId;
    this.confirmCancelId = null;
    this.http.delete(`${this.apiUrl}/Bookings/${bookingId}`).subscribe({
      next: () => {
        this.cancellingId = null;
        this.successMessage = 'Booking cancelled. A confirmation email has been sent.';
        this.loadMyBookings();
      },
      error: () => {
        this.cancellingId = null;
        this.errorMessage = 'Failed to cancel booking.';
      }
    });
  }

  isGroup(sessionType: string): boolean {
    return sessionType.toLowerCase().startsWith('group');
  }

  loadEnrolledModules() {
    this.loadingModules = true;
    this.http.get<any[]>(`${this.apiUrl}/enrollment/student/${this.userId}`).subscribe({
      next: (data) => {
        this.enrolledModules = data.map(e => ({
          module_Code: e.module_Code,
          module_Name: e.module_Name,
          can_Book_OneOnOne: e.can_Book_OneOnOne ?? true,
          can_Book_Group: e.can_Book_Group ?? true,
          sessions_Remaining_Group: e.sessions_Remaining_Group ?? 5,
          sessions_Remaining_OneOnOne: e.sessions_Remaining_OneOnOne ?? 5
        }));
        this.loadingModules = false;
      },
      error: () => { this.loadingModules = false; }
    });
  }

  onModuleChange() {
    this.selectedFormat = '';
    this.selectedSlotId = null;
    this.allSlots = [];
    this.filteredSlots = [];
    if (!this.selectedModule) return;
    this.loadingSlots = true;
    this.http.get<AvailableSlot[]>(`${this.apiUrl}/BookingSlots/available/module/${this.selectedModule}`).subscribe({
      next: (data) => { this.allSlots = data; this.loadingSlots = false; },
      error: () => { this.loadingSlots = false; }
    });
  }

  get selectedEnrollment(): EnrolledModule | undefined {
    return this.enrolledModules.find(m => m.module_Code === this.selectedModule);
  }

  canBookFormat(format: 'One-on-One' | 'Group'): boolean {
    if (!this.selectedEnrollment) return false;
    return format === 'One-on-One'
      ? this.selectedEnrollment.can_Book_OneOnOne
      : this.selectedEnrollment.can_Book_Group;
  }

  onFormatChange() {
    this.selectedSlotId = null;
    if (!this.selectedFormat) { this.filteredSlots = []; return; }
    this.filteredSlots = this.allSlots.filter(s =>
      s.session_Type.toLowerCase().startsWith(this.selectedFormat.toLowerCase())
    );
  }

  selectSlot(id: number) {
    this.selectedSlotId = this.selectedSlotId === id ? null : id;
  }

  getSelectedSlot(): AvailableSlot | undefined {
    return this.filteredSlots.find(s => s.booking_Slot_ID === this.selectedSlotId);
  }

  submitBooking() {
    if (!this.selectedSlotId) { this.errorMessage = 'Please select a time slot.'; return; }
    this.booking = true;
    this.errorMessage = '';
    this.http.post(`${this.apiUrl}/Bookings`, {
      student_ID: this.userId,
      booking_Slot_ID: this.selectedSlotId
    }).subscribe({
      next: () => {
        this.booking = false;
        this.successMessage = 'Session booked successfully!';
        this.loadEnrolledModules();
        this.resetForm();
      },
      error: (err) => {
        this.booking = false;
        const msg: string = err?.error ?? '';
        if (msg === 'NO_SESSIONS_GROUP') {
          this.sessionOutType = 'Group';
          this.sessionOutModule = this.selectedModule;
          this.showSessionOutModal = true;
        } else if (msg === 'NO_SESSIONS_ONEONONE') {
          this.sessionOutType = 'OneOnOne';
          this.sessionOutModule = this.selectedModule;
          this.showSessionOutModal = true;
        } else {
          this.errorMessage = msg || 'Failed to book session.';
        }
      }
    });
  }

  dismissSessionOutModal() {
    this.showSessionOutModal = false;
    this.sessionOutType = '';
    this.sessionOutModule = '';
  }

  get sessionOutModuleName(): string {
    return this.enrolledModules.find(m => m.module_Code === this.sessionOutModule)?.module_Name ?? this.sessionOutModule;
  }

  unenrolDueToNoPayment() {
    this.unenrolling = true;
    this.http.delete(`${this.apiUrl}/Enrollment/unenroll/${this.sessionOutModule}?studentId=${this.userId}`, {
      body: { unenroll_Reason: 'No sessions remaining — student declined top-up' }
    }).subscribe({
      next: () => {
        this.unenrolling = false;
        this.dismissSessionOutModal();
        this.successMessage = `You have been unenrolled from ${this.sessionOutModuleName}. Re-enrol any time to get 5 new sessions.`;
        this.resetForm();
        this.loadEnrolledModules();
      },
      error: () => {
        this.unenrolling = false;
        this.errorMessage = 'Failed to unenrol. Please try again.';
      }
    });
  }

  resetForm() {
    this.selectedModule = '';
    this.selectedFormat = '';
    this.selectedSlotId = null;
    this.allSlots = [];
    this.filteredSlots = [];
  }

  formatTime(t: string): string {
    try {
      const [h, m] = t.split(':').map(Number);
      const d = new Date(); d.setHours(h, m);
      return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
    } catch { return t; }
  }

  formatDate(d: string): string {
    try {
      return new Date(d + 'T00:00:00').toLocaleDateString('en-ZA', {
        weekday: 'short', day: 'numeric', month: 'short', year: 'numeric'
      });
    } catch { return d; }
  }

  getSessionMode(sessionType: string): string {
    const parts = sessionType.split('·');
    return parts.length > 1 ? parts[1].trim() : sessionType;
  }
}
