import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface MediaContent { media_ID: number; media_Name: string; media_Address: string; }

@Component({
  selector: 'app-media-viewer',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './media-viewer.component.html',
  styleUrl: './media-viewer.component.css'
})
export class MediaViewerComponent implements OnInit {
  mediaItems: MediaContent[] = [];
  loading = false;
  lightboxItem: MediaContent | null = null;

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnInit() { this.loadMedia(); }

  loadMedia() {
    this.loading = true;
    this.http.get<MediaContent[]>(`${this.apiUrl}/AdminContent/media`).subscribe({
      next: (data) => { this.mediaItems = data; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  get images(): MediaContent[] { return this.mediaItems.filter(m => this.isImage(m.media_Address)); }
  get videos(): MediaContent[] { return this.mediaItems.filter(m => this.isVideo(m.media_Address)); }
  get others(): MediaContent[] { return this.mediaItems.filter(m => !this.isImage(m.media_Address) && !this.isVideo(m.media_Address)); }

  openLightbox(item: MediaContent) { this.lightboxItem = item; }
  closeLightbox() { this.lightboxItem = null; }

  @HostListener('document:keydown.escape')
  onEscape() { this.closeLightbox(); }

  isImage(url: string): boolean { return /\.(jpg|jpeg|png|gif|webp|svg)(\?.*)?$/i.test(url) || url.includes('cloudinary') && !url.includes('.mp4') && !url.includes('.webm'); }
  isVideo(url: string): boolean { return /\.(mp4|webm|ogg)(\?.*)?$/i.test(url); }
}
