import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

interface LogHour {
  log_Hours_ID: number;
  log_Hours_Date: string;
  log_Hours_Time: string;
  log_Hours_Amount: number;
  tutor_ID: number;
  isApproved: boolean;
  approvalDate?: string;
}

@Component({
  selector: 'app-log-hours',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './log-hours.component.html',
  styleUrl: './log-hours.component.css'
})
export class LogHoursComponent implements OnInit {
  logs: LogHour[] = [];
  loading = false;
  errorMessage = '';
  successMessage = '';

  // Form fields
  showForm = false;
  editingLog: LogHour | null = null;
  formDate = '';
  formTime = '';
  formAmount: number | null = null;
  saving = false;

  // Delete confirmation
  deleteTargetId: number | null = null;

  private apiUrl = environment.apiUrl;
  private tutorId = 0;

  constructor(private http: HttpClient, private authService: AuthService) {}

  ngOnInit() {
    this.tutorId = this.authService.getCurrentUserId() ?? 0;
    this.loadLogs();
  }

  loadLogs() {
    this.loading = true;
    this.http.get<LogHour[]>(`${this.apiUrl}/LogHours/tutor/${this.tutorId}`).subscribe({
      next: (data) => { this.logs = data; this.loading = false; },
      error: () => { this.errorMessage = 'Failed to load log entries.'; this.loading = false; }
    });
  }

  openCreateForm() {
    this.editingLog = null;
    this.formDate = new Date().toISOString().split('T')[0];
    this.formTime = '09:00:00';
    this.formAmount = null;
    this.showForm = true;
    this.clearMessages();
  }

  openEditForm(log: LogHour) {
    this.editingLog = log;
    this.formDate = log.log_Hours_Date;
    this.formTime = log.log_Hours_Time;
    this.formAmount = log.log_Hours_Amount;
    this.clearMessages();
  }

  closeForm() {
    this.showForm = false;
    this.editingLog = null;
  }

  saveLog() {
    if (!this.formDate || !this.formTime || !this.formAmount) {
      this.errorMessage = 'Please fill in all fields.';
      return;
    }

    this.saving = true;
    const payload = {
      log_Hours_Date: this.formDate,
      log_Hours_Time: this.formTime,
      log_Hours_Amount: this.formAmount,
      tutor_ID: this.tutorId
    };

    const obs = this.editingLog
      ? this.http.put(`${this.apiUrl}/LogHours/${this.editingLog.log_Hours_ID}`, payload)
      : this.http.post(`${this.apiUrl}/LogHours`, payload);

    obs.subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = this.editingLog ? 'Log entry updated.' : 'Hours logged successfully.';
        this.closeForm();
        this.loadLogs();
      },
      error: () => {
        this.saving = false;
        this.errorMessage = 'Failed to save log entry.';
      }
    });
  }

  confirmDelete(id: number) {
    this.deleteTargetId = id;
  }

  cancelDelete() {
    this.deleteTargetId = null;
  }

  deleteLog() {
    if (!this.deleteTargetId) return;
    this.http.delete(`${this.apiUrl}/LogHours/${this.deleteTargetId}`).subscribe({
      next: () => {
        this.successMessage = 'Log entry deleted.';
        this.deleteTargetId = null;
        this.loadLogs();
      },
      error: () => { this.errorMessage = 'Failed to delete log entry.'; this.deleteTargetId = null; }
    });
  }

  get totalHours(): number {
    return this.logs.reduce((sum, l) => sum + l.log_Hours_Amount, 0);
  }

  get approvedHours(): number {
    return this.logs.filter(l => l.isApproved).reduce((sum, l) => sum + l.log_Hours_Amount, 0);
  }

  clearMessages() {
    this.errorMessage = '';
    this.successMessage = '';
  }

  formatDate(dateStr: string): string {
    try { return new Date(dateStr).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }); }
    catch { return dateStr; }
  }
}
