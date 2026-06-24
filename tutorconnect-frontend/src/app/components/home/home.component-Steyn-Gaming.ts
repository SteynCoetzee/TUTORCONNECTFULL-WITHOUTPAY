import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { Module, Testimonial } from '../../models/models';
import { environment } from '../../../environments/environment';

interface MediaContent { media_ID: number; media_Name: string; media_Address: string; }

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe, NgClass],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  userName = '';
  role = '';
  modules: Module[] = [];
  announcements: any[] = [];
  recentAnnouncements: any[] = [];
  mediaItems: MediaContent[] = [];
  testimonials: Testimonial[] = [];

  private apiUrl = environment.apiUrl;

  constructor(
    private authService: AuthService,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.userName = this.authService.getCurrentUserName();
    this.role = this.authService.getCurrentUserRole();

    this.http.get<Module[]>(`${this.apiUrl}/Modules`).subscribe({
      next: (data: Module[]) => { this.modules = data; }, error: () => {}
    });
    this.http.get<MediaContent[]>(`${this.apiUrl}/AdminContent/media`).subscribe({
      next: (data: MediaContent[]) => { this.mediaItems = data; }, error: () => {}
    });
    this.http.get<Testimonial[]>(`${this.apiUrl}/Testimonials/approved`).subscribe({
      next: (data: Testimonial[]) => { this.testimonials = data.slice(0, 4); }, error: () => {}
    });
  }

  getBadgeClass(type: string): string {
    const map: Record<string, string> = {
      'Update': 'badge badge-teal',
      'Deadline': 'badge badge-warning',
      'Event': 'badge badge-info',
      'Resource': 'badge badge-purple'
    };
    return map[type] || 'badge badge-teal';
  }

  getGreeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 17) return 'Good afternoon';
    return 'Good evening';
  }
}
