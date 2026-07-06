import { Component, ViewChild, ElementRef, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet, Router, NavigationStart, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { TopnavComponent } from '../topnav/topnav.component';
import { AuthService } from '../../services/auth.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopnavComponent, CommonModule],
  templateUrl: './dashboard-layout.component.html',
  styleUrl: './dashboard-layout.component.css'
})
export class DashboardLayoutComponent implements OnInit, OnDestroy {
  @ViewChild('contentArea') contentArea!: ElementRef<HTMLDivElement>;

  // First-login popups
  showCompleteProfilePopup = false;
  showAwaitingApprovalPopup = false;

  // Mobile sidebar
  sidebarMobileOpen = false;

  private apiUrl = environment.apiUrl;
  private activityDebounce: ReturnType<typeof setTimeout> | null = null;
  private readonly activityEvents = ['mousemove', 'keydown', 'click', 'touchstart', 'scroll'];
  private readonly onActivity = () => {
    if (this.activityDebounce) return;
    this.activityDebounce = setTimeout(() => { this.activityDebounce = null; }, 1000);
    this.authService.resetInactivityTimer();
  };

  constructor(
    private router: Router,
    private authService: AuthService,
    private http: HttpClient
  ) {
    this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) {
        const isBack = event.navigationTrigger === 'popstate';
        this.animateContent(isBack ? 'slide-from-left' : 'slide-from-right');
        this.sidebarMobileOpen = false;
      }
    });
  }

  ngOnInit() {
    this.activityEvents.forEach(e => window.addEventListener(e, this.onActivity, { passive: true }));
    this.loadAfkTimeout();
    const role = this.authService.getCurrentUserRole();
    const userId = this.authService.getCurrentUserId();
    if (!userId) return;

    if (role === 'AW-Tutor') {
      // Show approval notice first; complete-profile shows after they dismiss it
      this.showAwaitingApprovalPopup = true;
    } else if (role === 'Student') {
      this.checkStudentProfile(userId);
    }
  }

  private checkStudentProfile(userId: number) {
    this.http.get<any>(`${this.apiUrl}/Users/${userId}`).subscribe({
      next: (user) => {
        if (!user.firstName || !user.lastName || !user.phone) {
          this.showCompleteProfilePopup = true;
          return;
        }
        this.http.get<any>(`${this.apiUrl}/Users/${userId}/role-profile`).subscribe({
          next: (rp) => {
            if (!rp || !rp.student_Number) {
              this.showCompleteProfilePopup = true;
            }
          },
          error: () => { this.showCompleteProfilePopup = true; }
        });
      },
      error: () => { this.showCompleteProfilePopup = true; }
    });
  }

  dismissAwaitingPopup() {
    this.showAwaitingApprovalPopup = false;
    // After dismissing approval notice, prompt to complete profile
    this.showCompleteProfilePopup = true;
  }

  goToProfile() {
    this.showCompleteProfilePopup = false;
    this.router.navigate(['/dashboard/user-info']);
  }

  dismissPopup() {
    this.showCompleteProfilePopup = false;
    this.showAwaitingApprovalPopup = false;
  }

  ngOnDestroy() {
    this.activityEvents.forEach(e => window.removeEventListener(e, this.onActivity));
    if (this.activityDebounce) clearTimeout(this.activityDebounce);
    this.authService.stopInactivityTimer();
  }

  private loadAfkTimeout() {
    this.http.get<any[]>(`${this.apiUrl}/BusinessRules`).subscribe({
      next: (rules) => {
        const rule = rules.find(r => r.rule_Name === 'session_timeout_minutes');
        const minutes = rule ? Math.max(1, Number(rule.rule_Value)) : 30;
        this.authService.startInactivityTimer(minutes);
      },
      error: () => { this.authService.startInactivityTimer(30); }
    });
  }

  toggleMobileSidebar() { this.sidebarMobileOpen = !this.sidebarMobileOpen; }
  closeMobileSidebar() { this.sidebarMobileOpen = false; }

  private animateContent(cls: string) {
    const el = this.contentArea?.nativeElement;
    if (!el) return;
    el.classList.remove('slide-from-right', 'slide-from-left');
    void el.offsetWidth;
    el.classList.add(cls);
  }
}
