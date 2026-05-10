import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Testimonial {
  testimonial_ID: number;
  testimonial_Description: string;
  student_ID: number;
  testimonial_Category_ID: number;
  isApproved: boolean;
}

export interface TestimonialCategory {
  testimonial_Category_ID: number;
  test_Category_Name: string;
}

@Injectable({
  providedIn: 'root'
})
export class TestimonialService {
  private apiUrl = `${environment.apiUrl}/Testimonials`;

  constructor(private http: HttpClient) {}

  // Get all approved testimonials
  getTestimonials(): Observable<Testimonial[]> {
    return this.http.get<Testimonial[]>(this.apiUrl);
  }

  // Get testimonials by category
  getByCategory(categoryId: number): Observable<Testimonial[]> {
    return this.http.get<Testimonial[]>(`${this.apiUrl}/category/${categoryId}`);
  }

  // Create testimonial
  createTestimonial(payload: any): Observable<any> {
    return this.http.post(this.apiUrl, payload);
  }

  // Approve testimonial (admin only)
  approveTestimonial(id: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}/approve`, {});
  }

  // Delete testimonial
  deleteTestimonial(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  // Get categories
  getCategories(): Observable<TestimonialCategory[]> {
    return this.http.get<TestimonialCategory[]>(`${this.apiUrl}/categories`);
  }

  // Create category (admin only)
  createCategory(name: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/categories`, { test_Category_Name: name });
  }
}
