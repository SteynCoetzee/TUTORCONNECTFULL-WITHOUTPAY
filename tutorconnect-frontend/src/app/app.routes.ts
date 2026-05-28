import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { ForgotPasswordComponent } from './components/forgot-password/forgot-password.component';
import { LandingComponent } from './components/landing/landing.component';
import { DashboardLayoutComponent } from './components/dashboard-layout/dashboard-layout.component';
import { HomeComponent } from './components/home/home.component';
import { UserInfoComponent } from './components/user-info/user-info.component';
import { CoursesComponent } from './components/courses/courses.component';
import { AddModuleComponent } from './components/add-module/add-module.component';
import { AnnouncementsComponent } from './components/announcements/announcements.component';
import { BookingComponent } from './components/booking/booking.component';
import { ReportsComponent } from './components/reports/reports.component';
import { CalendarComponent } from './components/calendar/calendar.component';
import { LogHoursComponent } from './components/log-hours/log-hours.component';
import { ReviewsComponent } from './components/reviews/reviews.component';
import { AdminUsersComponent } from './components/admin-users/admin-users.component';
import { AdminFaqComponent } from './components/admin-faq/admin-faq.component';
import { AdminMediaComponent } from './components/admin-media/admin-media.component';
import { AdminHelpComponent } from './components/admin-help/admin-help.component';
import { AdminTestimonialsComponent } from './components/admin-testimonials/admin-testimonials.component';
import { BookingSlotsComponent } from './components/booking-slots/booking-slots.component';
import { AdminLogHoursComponent } from './components/admin-log-hours/admin-log-hours.component';
import { FaqViewerComponent } from './components/faq-viewer/faq-viewer.component';
import { HelpViewerComponent } from './components/help-viewer/help-viewer.component';
import { MediaViewerComponent } from './components/media-viewer/media-viewer.component';
import { TestimonialsViewerComponent } from './components/testimonials-viewer/testimonials-viewer.component';
import { ModuleDetailComponent } from './components/module-detail/module-detail.component';
import { ModuleWishlistComponent } from './components/module-wishlist/module-wishlist.component';
import { AdminAuditComponent } from './components/admin-audit/admin-audit.component';

export const routes: Routes = [
  // Public routes
  { path: '', component: LandingComponent },
  { path: 'home', component: LandingComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'help', component: HelpViewerComponent },
  { path: 'media', component: MediaViewerComponent },
  { path: 'testimonials', component: TestimonialsViewerComponent },

  // Protected dashboard routes
  {
    path: 'dashboard',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', component: HomeComponent },
      { path: 'user-info', component: UserInfoComponent },
      { path: 'courses', component: CoursesComponent },
      { path: 'module/:code', component: ModuleDetailComponent },
      { path: 'add-module', component: AddModuleComponent },
      { path: 'announcements', component: AnnouncementsComponent },
      { path: 'booking', component: BookingComponent },
      { path: 'calendar', component: CalendarComponent },
      { path: 'log-hours', component: LogHoursComponent },
      { path: 'reviews', component: ReviewsComponent },
      { path: 'slots', component: BookingSlotsComponent },
      { path: 'faqs', component: FaqViewerComponent },
      { path: 'wishlist', component: ModuleWishlistComponent },
      // Admin-only routes
      { path: 'reports', component: ReportsComponent },
      { path: 'users', component: AdminUsersComponent },
      { path: 'faq', component: AdminFaqComponent },
      { path: 'media', component: AdminMediaComponent },
      { path: 'help', component: AdminHelpComponent },
      { path: 'testimonials', component: AdminTestimonialsComponent },
      { path: 'log-hours-review', component: AdminLogHoursComponent },
      { path: 'audit-log', component: AdminAuditComponent },
    ]
  },

  // Wildcard
  { path: '**', redirectTo: '' }
];
