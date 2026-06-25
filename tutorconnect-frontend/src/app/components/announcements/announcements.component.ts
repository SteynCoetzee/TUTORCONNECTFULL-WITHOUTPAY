import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AnnouncementService } from '../../services/announcement.service';
import { AuthService } from '../../services/auth.service';
import { Announcement, AnnouncementCreate, AnnouncementUpdate } from '../../models/models';

@Component({
  selector: 'app-announcements',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, NgClass],
  templateUrl: './announcements.component.html',
  styleUrl: './announcements.component.css'
})
export class AnnouncementsComponent implements OnInit {
  activeTab: 'website' | 'module' = 'website';

  websiteAnnouncements: Announcement[] = [];
  moduleAnnouncements: Announcement[] = [];
  loading = false;
  isAdmin = false;

  // Error modal
  showErrorModal = false;
  errorModalMessage = '';

  // Create form (website announcements, inline at top)
  showCreateForm = false;
  createData: AnnouncementCreate = { announcement_Name: '', announcement_Details: '', announcement_Type: 'Update', module_Code: '' };
  creating = false;

  // Edit (inline)
  editingAnnouncement: Announcement | null = null;
  editData: AnnouncementUpdate = { announcement_Name: '', announcement_Details: '', announcement_Type: 'Update', module_Code: '' };
  updating = false;

  // Delete (inline)
  deletingId: number | null = null;
  deleting = false;

  types = ['Update', 'Deadline', 'Event', 'Resource'];

  constructor(
    private announcementService: AnnouncementService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    const role = this.authService.getCurrentUserRole();
    this.isAdmin = role === 'Admin';
    this.loadWebsite();
    this.loadModule();
  }

  loadWebsite() {
    this.loading = true;
    this.announcementService.getWebsiteAnnouncements().subscribe({
      next: (data) => { this.websiteAnnouncements = data; this.loading = false; },
      error: () => { this.loading = false; this.showError('Failed to load website announcements.'); }
    });
  }

  loadModule() {
    this.announcementService.getAnnouncements().subscribe({
      next: (data) => { this.moduleAnnouncements = data.filter(a => a.module_Code && a.module_Code.trim() !== ''); },
      error: () => {}
    });
  }

  // ── Create ────────────────────────────────────────────────────────
  openCreate() {
    this.createData = { announcement_Name: '', announcement_Details: '', announcement_Type: 'Update', module_Code: '' };
    this.showCreateForm = true;
    this.editingAnnouncement = null;
  }

  submitCreate() {
    if (!this.createData.announcement_Name) { this.showError('Title is required.'); return; }
    const userId = this.authService.getCurrentUserId();
    this.createData.admin_ID = userId ?? undefined;
    this.createData.module_Code = '';
    this.creating = true;
    this.announcementService.createAnnouncement(this.createData).subscribe({
      next: () => { this.creating = false; this.showCreateForm = false; this.loadWebsite(); },
      error: () => { this.creating = false; this.showError('Failed to create announcement.'); }
    });
  }

  // ── Edit ──────────────────────────────────────────────────────────
  openEdit(a: Announcement) {
    this.showCreateForm = false;
    this.editingAnnouncement = a;
    this.editData = {
      announcement_Name: a.announcement_Name,
      announcement_Details: a.announcement_Details,
      announcement_Type: a.announcement_Type,
      module_Code: a.module_Code
    };
  }

  cancelEdit() { this.editingAnnouncement = null; }

  submitEdit() {
    if (!this.editingAnnouncement) return;
    this.updating = true;
    this.announcementService.updateAnnouncement(this.editingAnnouncement.announcement_ID, this.editData).subscribe({
      next: () => { this.updating = false; this.editingAnnouncement = null; this.loadWebsite(); },
      error: () => { this.updating = false; this.showError('Failed to update announcement.'); }
    });
  }

  // ── Delete ────────────────────────────────────────────────────────
  openDelete(id: number) { this.deletingId = id; this.editingAnnouncement = null; this.showCreateForm = false; }
  cancelDelete() { this.deletingId = null; }

  confirmDelete() {
    if (this.deletingId === null) return;
    this.deleting = true;
    this.announcementService.deleteAnnouncement(this.deletingId).subscribe({
      next: () => { this.deleting = false; this.deletingId = null; this.loadWebsite(); },
      error: () => { this.deleting = false; this.showError('Failed to delete announcement.'); }
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────
  showError(msg: string) { this.errorModalMessage = msg; this.showErrorModal = true; }

  getBadgeClass(type: string): string {
    const map: Record<string, string> = {
      'Update': 'badge badge-teal',
      'Deadline': 'badge badge-warning',
      'Event': 'badge badge-info',
      'Resource': 'badge badge-purple'
    };
    return map[type] || 'badge badge-teal';
  }
}
