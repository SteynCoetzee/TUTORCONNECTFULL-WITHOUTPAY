import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface FAQ { faq_ID: number; question: string; answer: string; faq_Category_ID: number; FAQ_ID?: number; }
interface FAQCategory { faq_Category_ID: number; category_Name: string; }

@Component({
  selector: 'app-admin-faq',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-faq.component.html',
  styleUrl: './admin-faq.component.css'
})
export class AdminFaqComponent implements OnInit {
  faqs: FAQ[] = [];
  categories: FAQCategory[] = [];
  loading = false;
  successMessage = '';
  errorMessage = '';

  // FAQ form
  showFaqForm = false;
  editingFaq: FAQ | null = null;
  faqQuestion = '';
  faqAnswer = '';
  faqCategoryId: number | null = null;
  savingFaq = false;
  deleteFaqId: number | null = null;

  // Category form
  showCatForm = false;
  editingCat: FAQCategory | null = null;
  catName = '';
  savingCat = false;
  deleteCatId: number | null = null;

  activeTab: 'faqs' | 'categories' = 'faqs';
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

  getCategoryName(id: number): string {
    return this.categories.find(c => c.faq_Category_ID === id)?.category_Name ?? 'Unknown';
  }

  getFaqCountByCategory(categoryId: number): number {
    return this.faqs.filter(f => f.faq_Category_ID === categoryId).length;
  }

  closeFaqForm(): void {
    this.showFaqForm = false;
    this.editingFaq = null;
    this.faqQuestion = '';
    this.faqAnswer = '';
    this.faqCategoryId = null;
  }

  closeCatForm(): void {
    this.showCatForm = false;
    this.editingCat = null;
    this.catName = '';
  }

  // ─── FAQ CRUD ───────────────────────────────────────────────────────────────
  openFaqForm(faq?: FAQ) {
    this.editingFaq = faq ?? null;
    this.faqQuestion = faq?.question ?? '';
    this.faqAnswer = faq?.answer ?? '';
    this.faqCategoryId = faq?.faq_Category_ID ?? null;
    this.showFaqForm = true;
    this.clearMessages();
  }

  saveFaq() {
    if (!this.faqQuestion || !this.faqAnswer || !this.faqCategoryId) { this.errorMessage = 'Please fill in all fields.'; return; }
    this.savingFaq = true;
    const payload = { question: this.faqQuestion, answer: this.faqAnswer, FAQ_Category_ID: this.faqCategoryId };
    const obs = this.editingFaq
      ? this.http.put(`${this.apiUrl}/AdminContent/faqs/${this.editingFaq.faq_ID}`, payload)
      : this.http.post(`${this.apiUrl}/AdminContent/faqs`, payload);
    obs.subscribe({
      next: () => { this.savingFaq = false; this.successMessage = 'FAQ saved.'; this.showFaqForm = false; this.loadAll(); },
      error: () => { this.savingFaq = false; this.errorMessage = 'Failed to save FAQ.'; }
    });
  }

  deleteFaq() {
    if (!this.deleteFaqId) return;
    this.http.delete(`${this.apiUrl}/AdminContent/faqs/${this.deleteFaqId}`).subscribe({
      next: () => { this.successMessage = 'FAQ deleted.'; this.deleteFaqId = null; this.loadAll(); },
      error: () => { this.errorMessage = 'Failed to delete FAQ.'; this.deleteFaqId = null; }
    });
  }

  // ─── CATEGORY CRUD ──────────────────────────────────────────────────────────
  openCatForm(cat?: FAQCategory) {
    this.editingCat = cat ?? null;
    this.catName = cat?.category_Name ?? '';
    this.showCatForm = true;
    this.clearMessages();
  }

  saveCat() {
    if (!this.catName) { this.errorMessage = 'Category name is required.'; return; }
    this.savingCat = true;
    const payload = { category_Name: this.catName };
    const obs = this.editingCat
      ? this.http.put(`${this.apiUrl}/AdminContent/faq-categories/${this.editingCat.faq_Category_ID}`, payload)
      : this.http.post(`${this.apiUrl}/AdminContent/faq-categories`, payload);
    obs.subscribe({
      next: () => { this.savingCat = false; this.successMessage = 'Category saved.'; this.showCatForm = false; this.loadAll(); },
      error: () => { this.savingCat = false; this.errorMessage = 'Failed to save category.'; }
    });
  }

  deleteCat() {
    if (!this.deleteCatId) return;
    this.http.delete(`${this.apiUrl}/AdminContent/faq-categories/${this.deleteCatId}`).subscribe({
      next: () => { this.successMessage = 'Category deleted.'; this.deleteCatId = null; this.loadAll(); },
      error: () => { this.errorMessage = 'Failed to delete category.'; this.deleteCatId = null; }
    });
  }

  clearMessages() { this.errorMessage = ''; this.successMessage = ''; }
}
