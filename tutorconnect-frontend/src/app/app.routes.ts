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
import { LogHoursComponent } from './components/log-hours/log-hours.component';
import { AdminUsersComponent } from './components/admin-users/admin-users.component';
import { AdminFaqComponent } from './components/admin-faq/admin-faq.component';
import { AdminHelpComponent } from './components/admin-help/admin-help.component';
import { AdminTestimonialsComponent } from './components/admin-testimonials/admin-testimonials.component';
import { AdminLogHoursComponent } from './components/admin-log-hours/admin-log-hours.component';
import { FaqViewerComponent } from './components/faq-viewer/faq-viewer.component';
import { HelpViewerComponent } from './components/help-viewer/help-viewer.component';
import { TestimonialsViewerComponent } from './components/testimonials-viewer/testimonials-viewer.component';
import { ModuleDetailComponent } from './components/module-detail/module-detail.component';

export const routes: Routes = [
  { path: '', component: LandingComponent },
  { path: 'home', component: LandingComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'help', component: HelpViewerComponent },
  { path: 'testimonials', component: TestimonialsViewerComponent },
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
      { path: 'log-hours', component: LogHoursComponent },
      { path: 'users', component: AdminUsersComponent },
      { path: 'faq', component: AdminFaqComponent },
      { path: 'help', component: AdminHelpComponent },
      { path: 'testimonials', component: AdminTestimonialsComponent },
      { path: 'log-hours-review', component: AdminLogHoursComponent },
    ]
  },
  { path: '**', redirectTo: '' }
];
