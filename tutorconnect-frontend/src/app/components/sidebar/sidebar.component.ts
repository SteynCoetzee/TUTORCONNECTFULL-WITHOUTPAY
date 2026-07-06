import { Component, OnInit, Input, Output, EventEmitter, HostBinding } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
}

interface NavSection {
  heading?: string;
  items: NavItem[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent implements OnInit {
  sections: NavSection[] = [];
  role = '';
  collapsed = false;

  @Input() mobileOpen = false;
  @Output() mobileClose = new EventEmitter<void>();

  @HostBinding('class.mobile-open') get isMobileOpen() { return this.mobileOpen; }

  private studentSections: NavSection[] = [
    {
      heading: 'General',
      items: [
        { label: 'Home', icon: 'home', route: '/dashboard/home' },
        { label: 'My Profile', icon: 'person', route: '/dashboard/user-info' },
      ]
    },
    {
      heading: 'Learning',
      items: [
        { label: 'Modules', icon: 'menu_book', route: '/dashboard/courses' },
        { label: 'Announcements', icon: 'campaign', route: '/dashboard/announcements' },
        { label: 'Calendar', icon: 'calendar_today', route: '/dashboard/calendar' },
      ]
    },
    {
      heading: 'Sessions',
      items: [
        { label: 'Book a Session', icon: 'schedule', route: '/dashboard/booking' },
        { label: 'My Reviews', icon: 'star', route: '/dashboard/reviews' },
        { label: 'Testimonials', icon: 'star_border', route: '/dashboard/testimonials' },
      ]
    },
    {
      heading: 'Support',
      items: [
        { label: 'FAQs', icon: 'help_outline', route: '/dashboard/faqs' },
        { label: 'Module Wishlist', icon: 'favorite_border', route: '/dashboard/wishlist' },
      ]
    },
  ];

  private tutorSections: NavSection[] = [
    {
      heading: 'General',
      items: [
        { label: 'Home', icon: 'home', route: '/dashboard/home' },
        { label: 'My Profile', icon: 'person', route: '/dashboard/user-info' },
      ]
    },
    {
      heading: 'Teaching',
      items: [
        { label: 'Modules', icon: 'menu_book', route: '/dashboard/courses' },
        { label: 'Announcements', icon: 'campaign', route: '/dashboard/announcements' },
        { label: 'Calendar', icon: 'calendar_today', route: '/dashboard/calendar' },
      ]
    },
    {
      heading: 'Sessions',
      items: [
        { label: 'My Booking Slots', icon: 'event_available', route: '/dashboard/slots' },
        { label: 'Log Hours', icon: 'timer', route: '/dashboard/log-hours' },
        { label: 'Reviews', icon: 'star', route: '/dashboard/reviews' },
      ]
    },
    {
      heading: 'Support',
      items: [
        { label: 'FAQs', icon: 'help_outline', route: '/dashboard/faqs' },
      ]
    },
  ];

  private awTutorSections: NavSection[] = [
    {
      heading: 'Account',
      items: [
        { label: 'My Profile', icon: 'person', route: '/dashboard/user-info' },
      ]
    },
  ];

  private adminSections: NavSection[] = [
    {
      heading: 'General',
      items: [
        { label: 'Home', icon: 'home', route: '/dashboard/home' },
        { label: 'My Profile', icon: 'person', route: '/dashboard/user-info' },
      ]
    },
    {
      heading: 'Management',
      items: [
        { label: 'Users', icon: 'group', route: '/dashboard/users' },
        { label: 'Modules', icon: 'menu_book', route: '/dashboard/courses' },
        { label: 'Reports', icon: 'description', route: '/dashboard/reports' },
        { label: 'Announcements', icon: 'campaign', route: '/dashboard/announcements' },
      ]
    },
    {
      heading: 'Content',
      items: [
        { label: 'FAQ', icon: 'help', route: '/dashboard/faq' },
        { label: 'Media', icon: 'perm_media', route: '/dashboard/media' },
        { label: 'Help Page', icon: 'support', route: '/dashboard/help' },
        { label: 'Testimonials', icon: 'star_border', route: '/dashboard/testimonials' },
      ]
    },
    {
      heading: 'Reviews',
      items: [
        { label: 'Hours Review', icon: 'timer', route: '/dashboard/log-hours-review' },
        { label: 'Tutor & Session Reviews', icon: 'rate_review', route: '/dashboard/admin-reviews' },
      ]
    },
    {
      heading: 'System',
      items: [
        { label: 'Audit Log', icon: 'manage_search', route: '/dashboard/audit-log' },
        { label: 'Business Logic', icon: 'tune', route: '/dashboard/business-logic' },
      ]
    },
  ];

  constructor(private authService: AuthService) {}

  ngOnInit() {
    this.role = this.authService.getCurrentUserRole();
    if (this.role === 'Admin') {
      this.sections = this.adminSections;
    } else if (this.role === 'Tutor') {
      this.sections = this.tutorSections;
    } else if (this.role === 'AW-Tutor') {
      this.sections = this.awTutorSections;
    } else {
      this.sections = this.studentSections;
    }
  }

  toggle() {
    this.collapsed = !this.collapsed;
  }
}
