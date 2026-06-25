import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface AuditEntry {
  audit_Log_ID: number;
  audit_Date: string;
  audit_Time: string;
  user_ID: number;
  userName: string;
  transaction_Type: string;
  critical_Data: string;
}

@Component({
  selector: 'app-admin-audit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-audit.component.html',
  styleUrl: './admin-audit.component.css'
})
export class AdminAuditComponent implements OnInit {
  logs: AuditEntry[] = [];
  loading = false;
  filterType = '';
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnInit() { this.loadLogs(); }

  loadLogs() {
    this.loading = true;
    let url = `${this.apiUrl}/AuditLogs`;
    if (this.filterType) url += `?type=${encodeURIComponent(this.filterType)}`;
    this.http.get<AuditEntry[]>(url).subscribe({
      next: (data) => { this.logs = data; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  get transactionTypes(): string[] {
    return [...new Set(this.logs.map(l => l.transaction_Type))].sort();
  }

  formatTime(t: string): string {
    try { return t.substring(0, 5); } catch { return t; }
  }

  getBadgeClass(type: string): string {
    if (type.includes('Login') || type.includes('Registered')) return 'action-badge badge-blue';
    if (type.includes('Deleted') || type.includes('Rejected') || type.includes('Cancelled')) return 'action-badge badge-red';
    if (type.includes('Created') || type.includes('Enrolled') || type.includes('Booked') || type.includes('Submitted')) return 'action-badge badge-green';
    if (type.includes('Approved') || type.includes('Graded')) return 'action-badge badge-purple';
    if (type.includes('Changed') || type.includes('Updated') || type.includes('Unenrolled')) return 'action-badge badge-orange';
    return 'action-badge badge-gray';
  }
}
