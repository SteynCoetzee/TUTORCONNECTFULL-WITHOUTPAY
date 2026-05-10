import { Component, OnInit, HostListener } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';
import { Notification } from '../../models/models';
import { environment } from '../../../environments/environment';

interface AnnouncementNotif {
  announcement_ID: number;
  announcement_Name: string;
  announcement_Details: string;
  date_Posted: string;
  module_Code: string | null;
}

@Component({
  selector: 'app-topnav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './topnav.component.html',
  styleUrl: './topnav.component.css'
})
export class TopnavComponent implements OnInit {
  userName = '';
  role = '';
  notifications: Notification[] = [];
  announcements: AnnouncementNotif[] = [];
  unreadCount = 0;
  showNotifications = false;

  private apiUrl = environment.apiUrl;
  private seenKey = '';

  constructor(
    private authService: AuthService,
    private notificationService: NotificationService,
    private http: HttpClient,
    private router: Router
  ) {}

  ngOnInit() {
    this.userName = this.authService.getCurrentUserName();
    this.role = this.authService.getCurrentUserRole();
    const userId = this.authService.getCurrentUserId();
    this.seenKey = `seen_announcements_${userId}`;

    if (userId) {
      this.notificationService.getUserNotifications(userId).subscribe({
        next: (data) => { this.notifications = data; this.updateBadge(); },
        error: () => {}
      });

      if (this.role === 'Tutor') {
        this.http.get<AnnouncementNotif[]>(`${this.apiUrl}/Announcements/website`).subscribe({
          next: (data) => { this.announcements = data; this.updateBadge(); },
          error: () => {}
        });
      } else if (this.role === 'Student') {
        this.http.get<AnnouncementNotif[]>(`${this.apiUrl}/Announcements/student/${userId}`).subscribe({
          next: (data) => { this.announcements = data; this.updateBadge(); },
          error: () => {}
        });
      }
    }
  }

  private getSeenIds(): number[] {
    try { return JSON.parse(localStorage.getItem(this.seenKey) ?? '[]'); }
    catch { return []; }
  }

  private updateBadge() {
    const seen = this.getSeenIds();
    const unreadNotifs = this.notifications.filter(n => !n.is_Read).length;
    const unreadAnnounce = this.announcements.filter(a => !seen.includes(a.announcement_ID)).length;
    this.unreadCount = unreadNotifs + unreadAnnounce;
  }

  isAnnouncementNew(id: number): boolean {
    return !this.getSeenIds().includes(id);
  }

  toggleNotifications() {
    this.showNotifications = !this.showNotifications;
    if (this.showNotifications) {
      // Mark announcements as seen in localStorage
      const allIds = this.announcements.map(a => a.announcement_ID);
      localStorage.setItem(this.seenKey, JSON.stringify(allIds));

      // Mark all system notifications as read on the backend
      const userId = this.authService.getCurrentUserId();
      if (userId) {
        this.http.put(`${this.apiUrl}/Notifications/user/${userId}/read-all`, {}).subscribe({
          next: () => {
            this.notifications.forEach(n => n.is_Read = true);
            this.updateBadge();
          },
          error: () => {}
        });
      } else {
        this.updateBadge();
      }
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    if (!target.closest('.notif-wrapper')) {
      this.showNotifications = false;
    }
  }

  signOut() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  getInitials(): string {
    return this.userName ? this.userName.charAt(0).toUpperCase() : 'U';
  }
}
