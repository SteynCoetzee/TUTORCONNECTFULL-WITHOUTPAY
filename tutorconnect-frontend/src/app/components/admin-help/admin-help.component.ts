import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface HelpPage { help_Page_ID: number; help_Page_Title: string; help_Page_Description: string; }
interface HelpResource { help_Video_ID: number; video_Title: string; video_URL: string; }

@Component({
  selector: 'app-admin-help',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-help.component.html',
  styleUrl: './admin-help.component.css'
})
export class AdminHelpComponent implements OnInit {
  helpPages: HelpPage[] = [];
  helpResources: HelpResource[] = [];
  loading = false;
  successMessage = '';
  errorMessage = '';
  activeTab: 'pages' | 'resources' = 'pages';

  // Help page form
  showPageForm = false;
  editingPage: HelpPage | null = null;
  pageTitle = '';
  pageDescription = '';
  savingPage = false;
  deletePageId: number | null = null;

  // Help resource form
  showResourceForm = false;
  editingResource: HelpResource | null = null;
  resourceTitle = '';
  resourceUrl = '';
  savingResource = false;
  deleteResourceId: number | null = null;

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.loading = true;
    this.http.get<HelpPage[]>(`${this.apiUrl}/AdminContent/help-pages`).subscribe({
      next: (pages) => {
        this.helpPages = pages;
        this.http.get<HelpResource[]>(`${this.apiUrl}/AdminContent/help-resources`).subscribe({
          next: (res) => { this.helpResources = res; this.loading = false; },
          error: () => { this.loading = false; }
        });
      },
      error: () => { this.loading = false; }
    });
  }

  // ─── Help Pages ─────────────────────────────────────────────────────────────
  openPageForm(page?: HelpPage) {
    this.editingPage = page ?? null;
    this.pageTitle = page?.help_Page_Title ?? '';
    this.pageDescription = page?.help_Page_Description ?? '';
    this.showPageForm = true;
    this.clearMessages();
  }

  savePage() {
    if (!this.pageTitle || !this.pageDescription) { this.errorMessage = 'Please fill in all fields.'; return; }
    this.savingPage = true;
    const payload = { help_Page_Title: this.pageTitle, help_Page_Description: this.pageDescription };
    const obs = this.editingPage
      ? this.http.put(`${this.apiUrl}/AdminContent/help-pages/${this.editingPage.help_Page_ID}`, payload)
      : this.http.post(`${this.apiUrl}/AdminContent/help-pages`, payload);
    obs.subscribe({
      next: () => { this.savingPage = false; this.successMessage = 'Help page saved.'; this.showPageForm = false; this.loadAll(); },
      error: () => { this.savingPage = false; this.errorMessage = 'Failed to save help page.'; }
    });
  }

  deletePage() {
    if (!this.deletePageId) return;
    this.http.delete(`${this.apiUrl}/AdminContent/help-pages/${this.deletePageId}`).subscribe({
      next: () => { this.successMessage = 'Help page deleted.'; this.deletePageId = null; this.loadAll(); },
      error: () => { this.errorMessage = 'Failed to delete.'; this.deletePageId = null; }
    });
  }

  // ─── Help Resources ──────────────────────────────────────────────────────────
  openResourceForm(res?: HelpResource) {
    this.editingResource = res ?? null;
    this.resourceTitle = res?.video_Title ?? '';
    this.resourceUrl = res?.video_URL ?? '';
    this.showResourceForm = true;
    this.clearMessages();
  }

  saveResource() {
    if (!this.resourceTitle || !this.resourceUrl) { this.errorMessage = 'Please fill in all fields.'; return; }
    this.savingResource = true;
    const payload = { video_Title: this.resourceTitle, video_URL: this.resourceUrl };
    const obs = this.editingResource
      ? this.http.put(`${this.apiUrl}/AdminContent/help-resources/${this.editingResource.help_Video_ID}`, payload)
      : this.http.post(`${this.apiUrl}/AdminContent/help-resources`, payload);
    obs.subscribe({
      next: () => { this.savingResource = false; this.successMessage = 'Resource saved.'; this.showResourceForm = false; this.loadAll(); },
      error: () => { this.savingResource = false; this.errorMessage = 'Failed to save resource.'; }
    });
  }

  deleteResource() {
    if (!this.deleteResourceId) return;
    this.http.delete(`${this.apiUrl}/AdminContent/help-resources/${this.deleteResourceId}`).subscribe({
      next: () => { this.successMessage = 'Resource deleted.'; this.deleteResourceId = null; this.loadAll(); },
      error: () => { this.errorMessage = 'Failed to delete.'; this.deleteResourceId = null; }
    });
  }

  clearMessages() { this.errorMessage = ''; this.successMessage = ''; }
}
