import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

interface WishlistItem {
  wishlist_ID: number;
  module_Code: string;
  module_Name: string;
  student_ID: number;
  date_Submitted: string;
}

@Component({
  selector: 'app-module-wishlist',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './module-wishlist.component.html',
  styleUrl: './module-wishlist.component.css'
})
export class ModuleWishlistComponent implements OnInit {
  items: WishlistItem[] = [];
  loading = false;
  successMessage = '';
  errorMessage = '';

  formCode = '';
  formName = '';
  saving = false;

  confirmDeleteId: number | null = null;

  private apiUrl = environment.apiUrl;
  private userId = 0;

  constructor(private http: HttpClient, private authService: AuthService) {}

  ngOnInit() {
    this.userId = this.authService.getCurrentUserId() ?? 0;
    this.loadItems();
  }

  loadItems() {
    this.loading = true;
    this.http.get<WishlistItem[]>(`${this.apiUrl}/ModuleWishlist/student/${this.userId}`).subscribe({
      next: (data) => { this.items = data; this.loading = false; },
      error: () => { this.errorMessage = 'Failed to load wishlist.'; this.loading = false; }
    });
  }

  submit() {
    if (!this.formCode.trim() || !this.formName.trim()) {
      this.errorMessage = 'Please enter both a module code and module name.';
      return;
    }
    this.saving = true;
    this.clearMessages();
    this.http.post<WishlistItem>(`${this.apiUrl}/ModuleWishlist`, {
      module_Code: this.formCode.trim(),
      module_Name: this.formName.trim(),
      student_ID: this.userId
    }).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = 'Module added to your wishlist!';
        this.formCode = '';
        this.formName = '';
        this.loadItems();
      },
      error: (err) => {
        this.saving = false;
        this.errorMessage = err?.error || 'Failed to submit wishlist item.';
      }
    });
  }

  requestDelete(id: number) {
    this.confirmDeleteId = id;
    this.clearMessages();
  }

  cancelDelete() { this.confirmDeleteId = null; }

  confirmDelete() {
    if (!this.confirmDeleteId) return;
    this.http.delete(`${this.apiUrl}/ModuleWishlist/${this.confirmDeleteId}`).subscribe({
      next: () => {
        this.successMessage = 'Wishlist item removed.';
        this.confirmDeleteId = null;
        this.loadItems();
      },
      error: () => { this.errorMessage = 'Failed to remove item.'; this.confirmDeleteId = null; }
    });
  }

  clearMessages() { this.errorMessage = ''; this.successMessage = ''; }
}
