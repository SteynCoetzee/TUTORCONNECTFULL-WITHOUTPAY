import { Component } from '@angular/core';
import { RouterOutlet, Router, NavigationStart, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  slideDirection: 'from-right' | 'from-left' = 'from-right';

  constructor(private router: Router) {
    this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) {
        this.slideDirection = event.navigationTrigger === 'popstate' ? 'from-left' : 'from-right';
        // Apply direction class to body so child pages can read it
        document.body.classList.remove('nav-from-right', 'nav-from-left');
        document.body.classList.add(`nav-${this.slideDirection}`);
      }
    });
  }
}
