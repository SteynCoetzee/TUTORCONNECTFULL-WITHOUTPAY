import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

interface PendingLog {
  log_Hours_ID: number;
  log_Hours_Date: string;
  log_Hours_Time: string;
  log_Hours_Amount: number;
  tutor_ID: number;
  tutorName: string;
}

interface ApprovedLog {
  log_Hours_ID: number;
  log_Hours_Date: string;
  log_Hours_Time: string;
  log_Hours_Amount: number;
  tutor_ID: number;
  tutorName: string;
  isApproved: boolean;
  approvalDate: string | null;
  approvedBy_Admin_ID: number | null;
}

@Component({
  selector: 'app-admin-log-hours',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-log-hours.component.html',
  styleUrl: './admin-log-hours.component.css'
})
export class AdminLogHoursComponent implements OnInit {
  activeTab: 'pending' | 'approved' = 'pending';

  pendingLogs: PendingLog[] = [];
  approvedLogs: ApprovedLog[] = [];

  loadingPending = false;
  loadingApproved = false;

  successMessage = '';
  errorMessage = '';

  actionTargetId: number | null = null;
  actionType: 'approve' | 'reject' | null = null;

  private apiUrl = environment.apiUrl;
  private adminId = 0;

  constructor(private http: HttpClient, private authService: AuthService) {}

  ngOnInit() {
    this.adminId = this.authService.getCurrentUserId() ?? 0;
    this.loadPending();
    this.loadApproved();
  }

  loadPending() {
    this.loadingPending = true;
    this.http.get<PendingLog[]>(`${this.apiUrl}/LogHours/pending`).subscribe({
      next: (data) => { this.pendingLogs = data; this.loadingPending = false; },
      error: () => { this.errorMessage = 'Failed to load pending logs.'; this.loadingPending = false; }
    });
  }

  loadApproved() {
    this.loadingApproved = true;
    this.http.get<ApprovedLog[]>(`${this.apiUrl}/LogHours`).subscribe({
      next: (data) => {
        this.approvedLogs = data.filter(l => l.isApproved);
        this.loadingApproved = false;
      },
      error: () => { this.loadingApproved = false; }
    });
  }

  confirmAction(id: number, type: 'approve' | 'reject') {
    this.actionTargetId = id;
    this.actionType = type;
    this.clearMessages();
  }

  cancelAction() {
    this.actionTargetId = null;
    this.actionType = null;
  }

  executeAction() {
    if (!this.actionTargetId || !this.actionType) return;
    const id = this.actionTargetId;
    const type = this.actionType;
    this.actionTargetId = null;
    this.actionType = null;

    if (type === 'approve') {
      this.http.put(`${this.apiUrl}/LogHours/${id}/approve`, this.adminId).subscribe({
        next: () => {
          this.successMessage = 'Hours approved successfully.';
          this.loadPending();
          this.loadApproved();
        },
        error: () => { this.errorMessage = 'Failed to approve hours.'; }
      });
    } else {
      this.http.delete(`${this.apiUrl}/LogHours/${id}/reject`).subscribe({
        next: () => {
          this.successMessage = 'Log entry deleted.';
          this.loadPending();
          this.loadApproved();
        },
        error: () => { this.errorMessage = 'Failed to reject entry.'; }
      });
    }
  }

  formatDate(d: string): string {
    try { return new Date(d).toLocaleDateString('en-ZA', { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' }); }
    catch { return d; }
  }

  formatTime(t: string): string {
    try {
      const [h, m] = t.split(':').map(Number);
      const d = new Date(); d.setHours(h, m);
      return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
    } catch { return t; }
  }

  clearMessages() { this.successMessage = ''; this.errorMessage = ''; }
}
