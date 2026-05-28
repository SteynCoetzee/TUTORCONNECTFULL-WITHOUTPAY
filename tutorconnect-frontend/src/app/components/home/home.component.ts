import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { ModuleService } from '../../services/module.service';
import { AnnouncementService } from '../../services/announcement.service';
import { Module, Announcement, Testimonial } from '../../models/models';
import { environment } from '../../../environments/environment';

interface MediaContent { media_ID: number; media_Name: string; media_Address: string; }
interface WishlistItem { wishlist_ID: number; module_Code: string; module_Name: string; date_Submitted: string; }

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
  announcements: Announcement[] = [];
  recentAnnouncements: Announcement[] = [];
  mediaItems: MediaContent[] = [];
  testimonials: Testimonial[] = [];
  wishlistItems: WishlistItem[] = [];

  private apiUrl = environment.apiUrl;

  constructor(
    private authService: AuthService,
    private moduleService: ModuleService,
    private announcementService: AnnouncementService,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.userName = this.authService.getCurrentUserName();
    this.role = this.authService.getCurrentUserRole();

    this.moduleService.getModules().subscribe({ next: (data) => { this.modules = data; }, error: () => {} });
    this.announcementService.getAnnouncements().subscribe({
      next: (data) => {
        this.announcements = data;
        this.recentAnnouncements = data.slice(0, 3);
      },
      error: () => {}
    });
    this.http.get<MediaContent[]>(`${this.apiUrl}/AdminContent/media`).subscribe({
      next: (data) => { this.mediaItems = data; },
      error: () => {}
    });
    this.http.get<Testimonial[]>(`${this.apiUrl}/Testimonials/approved`).subscribe({
      next: (data) => { this.testimonials = data.slice(0, 4); },
      error: () => {}
    });

    // Load wishlist for students
    const userId = this.authService.getCurrentUserId();
    if (this.role === 'Student' && userId) {
      this.http.get<WishlistItem[]>(`${this.apiUrl}/ModuleWishlist/student/${userId}`).subscribe({
        next: (data) => { this.wishlistItems = data; },
        error: () => {}
      });
    }
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
