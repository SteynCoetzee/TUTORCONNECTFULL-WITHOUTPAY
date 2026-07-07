import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-payment-result',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './payment-result.component.html',
  styleUrl: './payment-result.component.css'
})
export class PaymentResultComponent implements OnInit {
  status: 'success' | 'cancel' | 'unknown' = 'unknown';

  constructor(private route: ActivatedRoute, private router: Router) {}

  ngOnInit() {
    const s = this.route.snapshot.queryParamMap.get('status');
    this.status = s === 'success' ? 'success' : s === 'cancel' ? 'cancel' : 'unknown';
  }

  goToModules() {
    this.router.navigate(['/dashboard/courses']);
  }
}
