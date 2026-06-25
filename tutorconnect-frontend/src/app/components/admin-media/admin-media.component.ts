import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface MediaContent { media_ID: number; media_Name: string; media_Address: string; }

@Component({
  selector: 'app-admin-media',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-media.component.html',
  styleUrl: './admin-media.component.css'
})
export class AdminMediaComponent implements OnInit {
  mediaItems: MediaContent[] = [];
  loading = false;
  successMessage = '';
  errorMessage = '';

  // Inline create form
  showCreateForm = false;
  createName = '';
  createAddress = '';
  createPreview: string | null = null;
  createFile: File | null = null;
  creating = false;

  // Inline edit
  editingItem: MediaContent | null = null;
  editName = '';
  editAddress = '';
  editPreview: string | null = null;
  editFile: File | null = null;
  updating = false;

  // Delete
  deleteTargetId: number | null = null;

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnInit() { this.loadMedia(); }

  loadMedia() {
    this.loading = true;
    this.http.get<MediaContent[]>(`${this.apiUrl}/AdminContent/media`).subscribe({
      next: (data) => { this.mediaItems = data; this.loading = false; },
      error: () => { this.errorMessage = 'Failed to load media.'; this.loading = false; }
    });
  }

  // ── Create ──────────────────────────────────────────────────────────────────

  openCreate() {
    this.showCreateForm = true;
    this.createName = '';
    this.createAddress = '';
    this.createPreview = null;
    this.createFile = null;
    this.editingItem = null;
    this.clearMessages();
  }

  private validateMedia(file: File): string | null {
    const MB = 1024 * 1024;
    const imgMimes   = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
    const videoMimes = ['video/mp4', 'video/webm', 'video/quicktime'];
    if (imgMimes.includes(file.type)) {
      if (file.size > 10 * MB) return 'Image must be under 10 MB.';
      return null;
    }
    if (videoMimes.includes(file.type)) {
      if (file.size > 500 * MB) return 'Video must be under 500 MB.';
      return null;
    }
    return 'Only images (JPG, PNG, GIF, WebP) or videos (MP4, WebM, MOV) are allowed.';
  }

  onCreateFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const err = this.validateMedia(file);
    if (err) { this.errorMessage = err; input.value = ''; return; }
    this.createFile = file;
    this.createAddress = '';
    this.errorMessage = '';
    const reader = new FileReader();
    reader.onload = (e) => { this.createPreview = e.target?.result as string; };
    reader.readAsDataURL(file);
  }

  clearCreateFile() {
    this.createFile = null;
    this.createPreview = null;
  }

  create() {
    if (!this.createName) { this.errorMessage = 'Name is required.'; return; }
    if (!this.createFile && !this.createAddress) { this.errorMessage = 'Please upload a file or enter a URL.'; return; }
    this.creating = true;
    if (this.createFile) {
      this.uploadFile(this.createFile, (url) => {
        this.saveCreate(url);
      });
    } else {
      this.saveCreate(this.createAddress);
    }
  }

  private saveCreate(address: string) {
    this.http.post(`${this.apiUrl}/AdminContent/media`, { media_Name: this.createName, media_Address: address }).subscribe({
      next: () => {
        this.creating = false;
        this.showCreateForm = false;
        this.successMessage = 'Media added.';
        this.loadMedia();
      },
      error: () => { this.creating = false; this.errorMessage = 'Failed to add media.'; }
    });
  }

  // ── Edit ────────────────────────────────────────────────────────────────────

  openEdit(item: MediaContent) {
    this.showCreateForm = false;
    this.deleteTargetId = null;
    this.editingItem = item;
    this.editName = item.media_Name;
    this.editAddress = item.media_Address;
    this.editPreview = this.isImage(item.media_Address) ? item.media_Address : null;
    this.editFile = null;
    this.clearMessages();
  }

  onEditFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const err = this.validateMedia(file);
    if (err) { this.errorMessage = err; input.value = ''; return; }
    this.editFile = file;
    this.editAddress = '';
    this.errorMessage = '';
    const reader = new FileReader();
    reader.onload = (e) => { this.editPreview = e.target?.result as string; };
    reader.readAsDataURL(file);
  }

  clearEditFile() {
    this.editFile = null;
    this.editPreview = this.editingItem && this.isImage(this.editingItem.media_Address)
      ? this.editingItem.media_Address : null;
    this.editAddress = this.editingItem?.media_Address ?? '';
  }

  saveEdit() {
    if (!this.editingItem) return;
    if (!this.editName) { this.errorMessage = 'Name is required.'; return; }
    if (!this.editFile && !this.editAddress) { this.errorMessage = 'Please upload a file or enter a URL.'; return; }
    this.updating = true;
    if (this.editFile) {
      this.uploadFile(this.editFile, (url) => { this.submitEdit(url); });
    } else {
      this.submitEdit(this.editAddress);
    }
  }

  private submitEdit(address: string) {
    this.http.put(`${this.apiUrl}/AdminContent/media/${this.editingItem!.media_ID}`,
      { media_Name: this.editName, media_Address: address }).subscribe({
      next: () => {
        this.updating = false;
        this.editingItem = null;
        this.successMessage = 'Media updated.';
        this.loadMedia();
      },
      error: () => { this.updating = false; this.errorMessage = 'Failed to update media.'; }
    });
  }

  cancelEdit() { this.editingItem = null; this.clearMessages(); }

  // ── Delete ──────────────────────────────────────────────────────────────────

  confirmDelete(id: number) { this.deleteTargetId = id; this.editingItem = null; this.showCreateForm = false; }
  cancelDelete() { this.deleteTargetId = null; }

  deleteMedia() {
    if (!this.deleteTargetId) return;
    this.http.delete(`${this.apiUrl}/AdminContent/media/${this.deleteTargetId}`).subscribe({
      next: () => { this.successMessage = 'Media deleted.'; this.deleteTargetId = null; this.loadMedia(); },
      error: () => { this.errorMessage = 'Failed to delete media.'; this.deleteTargetId = null; }
    });
  }

  // ── Helpers ─────────────────────────────────────────────────────────────────

  private uploadFile(file: File, callback: (url: string) => void) {
    const formData = new FormData();
    formData.append('file', file);
    this.http.post(`${this.apiUrl}/AdminContent/media/upload`, formData, { responseType: 'text' }).subscribe({
      next: (url) => { callback(url.replace(/^"|"$/g, '')); },
      error: (err) => {
        this.creating = false;
        this.updating = false;
        this.errorMessage = err?.error || err?.message || 'File upload failed.';
      }
    });
  }

  getFullImageUrl(path: string): string {
    if (path.startsWith('http') || path.startsWith('data:')) return path;
    return `${this.apiUrl.replace('/api', '')}${path}`;
  }

  isImage(url: string): boolean { return /\.(jpg|jpeg|png|gif|webp|svg)$/i.test(url) || url.startsWith('data:image'); }
  isVideo(url: string): boolean { return /\.(mp4|webm|ogg)$/i.test(url); }
  clearMessages() { this.errorMessage = ''; this.successMessage = ''; }
}
