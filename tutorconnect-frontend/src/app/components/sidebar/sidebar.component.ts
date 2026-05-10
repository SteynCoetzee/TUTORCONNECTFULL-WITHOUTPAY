import { Component, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

interface NavItem { label: string; icon: string; route: string; }
interface NavSection { heading?: string; items: NavItem[]; }

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
        { label: 'Log Hours', icon: 'timer', route: '/dashboard/log-hours' },
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
      ]
    },
    {
      heading: 'Content',
      items: [
        { label: 'FAQ', icon: 'help', route: '/dashboard/faq' },
        { label: 'Help Page', icon: 'support', route: '/dashboard/help' },
        { label: 'Testimonials', icon: 'star_border', route: '/dashboard/testimonials' },
      ]
    },
    {
      heading: 'Reviews',
      items: [
        { label: 'Hours Review', icon: 'timer', route: '/dashboard/log-hours-review' },
      ]
    },
  ];

  constructor(private authService: AuthService) {}

  ngOnInit() {
    this.role = this.authService.getCurrentUserRole();
    if (this.role === 'Admin') this.sections = this.adminSections;
    else if (this.role === 'Tutor') this.sections = this.tutorSections;
    else if (this.role === 'AW-Tutor') this.sections = this.awTutorSections;
    else this.sections = this.studentSections;
  }

  toggle() { this.collapsed = !this.collapsed; }
}
