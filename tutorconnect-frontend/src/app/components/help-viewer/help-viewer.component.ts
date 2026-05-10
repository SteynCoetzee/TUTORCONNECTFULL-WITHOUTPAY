import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface HelpPage { help_Page_ID: number; help_Page_Title: string; help_Page_Description: string; }
interface HelpResource { help_Video_ID: number; video_Title: string; video_URL: string; }

@Component({
  selector: 'app-help-viewer',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './help-viewer.component.html',
  styleUrl: './help-viewer.component.css'
})
export class HelpViewerComponent implements OnInit {
  helpPages: HelpPage[] = [];
  helpResources: HelpResource[] = [];
  loading = false;
  expandedPageId: number | null = null;
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

  toggle(id: number) {
    this.expandedPageId = this.expandedPageId === id ? null : id;
  }
}
