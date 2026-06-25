import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

interface CalendarBooking {
  booking_ID: number;
  booking_Slot_ID: number;
  slot_Date: string;
  slot_Time: string;
  session_Type: string;
  tutorName: string;
  location: string;
  moduleCode: string;
}

interface AvailableSlot {
  booking_Slot_ID: number;
  slot_Date: string;
  slot_Time: string;
  session_Type: string;
  location: string;
  tutorName: string;
  moduleCode: string;
  remaining: number;
}

interface CalendarSlot {
  booking_Slot_ID: number;
  slot_Date: string;
  slot_Time: string;
  session_Type: string;
  is_Booked: boolean;
  tutor_ID: number;
  tutorName: string;
}

@Component({
  selector: 'app-calendar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './calendar.component.html',
  styleUrl: './calendar.component.css'
})
export class CalendarComponent implements OnInit {
  currentMonth: Date = new Date();
  calendarDays: (Date | null)[] = [];
  loading = false;
  errorMessage = '';
  successMessage = '';

  selectedDay: Date | null = null;

  // Student
  bookings: CalendarBooking[] = [];
  dayBookings: CalendarBooking[] = [];
  availableSlots: AvailableSlot[] = [];
  dayAvailableSlots: AvailableSlot[] = [];

  // Tutor / Admin
  slots: CalendarSlot[] = [];
  daySlots: CalendarSlot[] = [];

  confirmCancelId: number | null = null;
  cancelling = false;

  userRole = '';
  private apiUrl = environment.apiUrl;
  private userId = 0;

  constructor(private http: HttpClient, private authService: AuthService) {}

  ngOnInit() {
    this.userId = this.authService.getCurrentUserId() ?? 0;
    this.userRole = this.authService.getCurrentUserRole();
    this.buildCalendar(this.currentMonth);
    if (this.userRole === 'Student') {
      this.loadBookings();
      this.loadAvailableSlots();
    } else if (this.userRole === 'Tutor') {
      this.loadTutorSlots();
    } else {
      this.loadAllSlots();
    }
  }

  loadBookings() {
    this.http.get<any[]>(`${this.apiUrl}/Bookings/student/${this.userId}`).subscribe({
      next: (data) => {
        this.bookings = data.map(b => ({
          booking_ID:      b.booking_ID,
          booking_Slot_ID: b.booking_Slot_ID,
          slot_Date:       b.slotDate ?? '',
          slot_Time:       b.slotTime ?? '',
          session_Type:    b.sessionType ?? '',
          tutorName:       b.tutorName ?? '',
          location:        b.location ?? '',
          moduleCode:      b.moduleCode ?? ''
        })).filter(b => b.slot_Date !== '');
        if (this.selectedDay) this.refreshDayView();
      },
      error: () => { this.errorMessage = 'Failed to load bookings.'; }
    });
  }

  loadAvailableSlots() {
    this.loading = true;
    this.http.get<any[]>(`${this.apiUrl}/BookingSlots/available/student/${this.userId}`).subscribe({
      next: (data) => {
        this.availableSlots = data.map(s => ({
          booking_Slot_ID: s.booking_Slot_ID,
          slot_Date:       s.slotDate ?? '',
          slot_Time:       s.slotTime ?? '',
          session_Type:    s.session_Type ?? '',
          location:        s.location ?? '',
          tutorName:       s.tutorName ?? '',
          moduleCode:      s.module_Code ?? '',
          remaining:       s.remaining ?? 1
        })).filter(s => s.slot_Date !== '');
        this.loading = false;
        if (this.selectedDay) this.refreshDayView();
      },
      error: () => { this.loading = false; }
    });
  }

  loadTutorSlots() {
    this.loading = true;
    this.http.get<CalendarSlot[]>(`${this.apiUrl}/BookingSlots/tutor/${this.userId}`).subscribe({
      next: (data) => { this.slots = data; this.loading = false; },
      error: () => { this.errorMessage = 'Failed to load slots.'; this.loading = false; }
    });
  }

  loadAllSlots() {
    this.loading = true;
    this.http.get<CalendarSlot[]>(`${this.apiUrl}/BookingSlots`).subscribe({
      next: (data) => { this.slots = data; this.loading = false; },
      error: () => { this.errorMessage = 'Failed to load slots.'; this.loading = false; }
    });
  }

  // ─── Calendar navigation ─────────────────────────────────────────────────────
  buildCalendar(month: Date) {
    this.calendarDays = [];
    const year = month.getFullYear();
    const m = month.getMonth();
    const firstDay = new Date(year, m, 1).getDay();
    const daysInMonth = new Date(year, m + 1, 0).getDate();
    for (let i = 0; i < firstDay; i++) this.calendarDays.push(null);
    for (let d = 1; d <= daysInMonth; d++) this.calendarDays.push(new Date(year, m, d));
  }

  prevMonth() {
    this.currentMonth = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() - 1, 1);
    this.buildCalendar(this.currentMonth);
    this.clearSelection();
  }

  nextMonth() {
    this.currentMonth = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() + 1, 1);
    this.buildCalendar(this.currentMonth);
    this.clearSelection();
  }

  private clearSelection() {
    this.selectedDay = null;
    this.dayBookings = [];
    this.dayAvailableSlots = [];
    this.daySlots = [];
    this.confirmCancelId = null;
  }

  selectDay(day: Date | null) {
    if (!day) return;
    this.selectedDay = day;
    this.confirmCancelId = null;
    this.refreshDayView();
  }

  private refreshDayView() {
    if (!this.selectedDay) return;
    const dateStr = this.toLocalDateStr(this.selectedDay);
    if (this.userRole === 'Student') {
      this.dayBookings      = this.bookings.filter(b => b.slot_Date.startsWith(dateStr));
      this.dayAvailableSlots = this.availableSlots.filter(s => s.slot_Date.startsWith(dateStr));
    } else {
      this.daySlots = this.slots.filter(s => s.slot_Date.startsWith(dateStr));
    }
  }

  // ─── Cancel booking ──────────────────────────────────────────────────────────
  requestCancel(bookingId: number) {
    this.confirmCancelId = bookingId;
    this.errorMessage = '';
    this.successMessage = '';
  }

  dismissCancel() { this.confirmCancelId = null; }

  executeCancel() {
    if (!this.confirmCancelId) return;
    const id = this.confirmCancelId;
    this.cancelling = true;
    this.http.delete(`${this.apiUrl}/Bookings/${id}`).subscribe({
      next: () => {
        this.cancelling = false;
        this.confirmCancelId = null;
        this.successMessage = 'Booking cancelled. The slot is now available again.';
        this.loadBookings();
        this.loadAvailableSlots();
      },
      error: (err) => {
        this.cancelling = false;
        this.confirmCancelId = null;
        this.errorMessage = err?.error || 'Failed to cancel booking.';
      }
    });
  }

  // ─── Day indicators ──────────────────────────────────────────────────────────
  hasBooking(day: Date | null): boolean {
    if (!day) return false;
    const dateStr = this.toLocalDateStr(day);
    if (this.userRole === 'Student') return this.bookings.some(b => b.slot_Date.startsWith(dateStr));
    return this.slots.some(s => s.slot_Date.startsWith(dateStr));
  }

  hasAvailableForStudent(day: Date | null): boolean {
    if (!day || this.userRole !== 'Student') return false;
    const dateStr = this.toLocalDateStr(day);
    return this.availableSlots.some(s => s.slot_Date.startsWith(dateStr));
  }

  hasAvailableSlot(day: Date | null): boolean {
    if (!day || this.userRole === 'Student') return false;
    const dateStr = this.toLocalDateStr(day);
    return this.slots.some(s => s.slot_Date.startsWith(dateStr) && !s.is_Booked);
  }

  hasBookedSlot(day: Date | null): boolean {
    if (!day || this.userRole === 'Student') return false;
    const dateStr = this.toLocalDateStr(day);
    return this.slots.some(s => s.slot_Date.startsWith(dateStr) && s.is_Booked);
  }

  // ─── Upcoming ────────────────────────────────────────────────────────────────
  get upcomingBookings(): CalendarBooking[] {
    const today = this.toLocalDateStr(new Date());
    return this.bookings
      .filter(b => b.slot_Date >= today)
      .sort((a, b) => a.slot_Date.localeCompare(b.slot_Date));
  }

  get upcomingAvailable(): AvailableSlot[] {
    const today = this.toLocalDateStr(new Date());
    return this.availableSlots
      .filter(s => s.slot_Date >= today)
      .sort((a, b) => a.slot_Date.localeCompare(b.slot_Date))
      .slice(0, 5);
  }

  get upcomingSlots(): CalendarSlot[] {
    const today = this.toLocalDateStr(new Date());
    return this.slots
      .filter(s => s.slot_Date >= today)
      .sort((a, b) => a.slot_Date.localeCompare(b.slot_Date))
      .slice(0, 8);
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────────
  isToday(day: Date | null): boolean {
    if (!day) return false;
    return day.toDateString() === new Date().toDateString();
  }

  isSelected(day: Date | null): boolean {
    if (!day || !this.selectedDay) return false;
    return day.toDateString() === this.selectedDay.toDateString();
  }

  getMonthName(): string {
    return this.currentMonth.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  }

  formatTime(timeStr: string): string {
    try {
      const [h, m] = timeStr.split(':').map(Number);
      const date = new Date();
      date.setHours(h, m);
      return date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
    } catch { return timeStr; }
  }

  private toLocalDateStr(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
