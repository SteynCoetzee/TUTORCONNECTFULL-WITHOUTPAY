import { Component, ViewChild, ElementRef } from '@angular/core';
import { RouterOutlet, Router, NavigationStart, NavigationEnd } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { TopnavComponent } from '../topnav/topnav.component';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopnavComponent],
  templateUrl: './dashboard-layout.component.html',
  styleUrl: './dashboard-layout.component.css'
})
export class DashboardLayoutComponent {
  @ViewChild('contentArea') contentArea!: ElementRef<HTMLDivElement>;

  constructor(private router: Router) {
    this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) {
        const isBack = event.navigationTrigger === 'popstate';
        this.animateContent(isBack ? 'slide-from-left' : 'slide-from-right');
      }
    });
  }

  private animateContent(cls: string) {
    const el = this.contentArea?.nativeElement;
    if (!el) return;
    el.classList.remove('slide-from-right', 'slide-from-left');
    // Force reflow so removing+re-adding the class restarts the animation
    void el.offsetWidth;
    el.classList.add(cls);
  }
}
