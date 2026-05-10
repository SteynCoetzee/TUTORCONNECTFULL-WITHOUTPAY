import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface Testimonial {
  testimonial_ID: number;
  testimonial_Description: string;
  testimonial_Category_ID: number;
}

interface TestimonialCategory {
  testimonial_Category_ID: number;
  test_Category_Name: string;
}

@Component({
  selector: 'app-testimonials-viewer',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './testimonials-viewer.component.html',
  styleUrl: './testimonials-viewer.component.css'
})
export class TestimonialsViewerComponent implements OnInit {
  testimonials: Testimonial[] = [];
  categories: TestimonialCategory[] = [];
  loading = false;
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.loading = true;
    this.http.get<TestimonialCategory[]>(`${this.apiUrl}/Testimonials/categories`).subscribe({
      next: (cats) => {
        this.categories = cats;
        this.http.get<Testimonial[]>(`${this.apiUrl}/Testimonials/approved`).subscribe({
          next: (data) => { this.testimonials = data; this.loading = false; },
          error: () => { this.loading = false; }
        });
      },
      error: () => { this.loading = false; }
    });
  }

  getCategoryName(id: number): string {
    return this.categories.find(c => c.testimonial_Category_ID === id)?.test_Category_Name ?? '';
  }
}
