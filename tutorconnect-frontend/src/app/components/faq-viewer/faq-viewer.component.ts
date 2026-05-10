import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface FAQ { faq_ID: number; question: string; answer: string; faq_Category_ID: number; }
interface FAQCategory { faq_Category_ID: number; category_Name: string; }

@Component({
  selector: 'app-faq-viewer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './faq-viewer.component.html',
  styleUrl: './faq-viewer.component.css'
})
export class FaqViewerComponent implements OnInit {
  faqs: FAQ[] = [];
  categories: FAQCategory[] = [];
  loading = false;
  expandedFaqId: number | null = null;
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.loading = true;
    this.http.get<FAQCategory[]>(`${this.apiUrl}/AdminContent/faq-categories`).subscribe({
      next: (cats) => {
        this.categories = cats;
        this.http.get<FAQ[]>(`${this.apiUrl}/AdminContent/faqs`).subscribe({
          next: (f) => { this.faqs = f; this.loading = false; },
          error: () => { this.loading = false; }
        });
      },
      error: () => { this.loading = false; }
    });
  }

  getFaqsByCategory(categoryId: number): FAQ[] {
    return this.faqs.filter(f => f.faq_Category_ID === categoryId);
  }

  toggle(id: number) {
    this.expandedFaqId = this.expandedFaqId === id ? null : id;
  }
}
