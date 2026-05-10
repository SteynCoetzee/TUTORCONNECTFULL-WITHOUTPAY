import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { Module } from '../../models/models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-add-module',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-module.component.html',
  styleUrl: './add-module.component.css'
})
export class AddModuleComponent implements OnInit {
  isEditMode = false;
  editingModuleCode = '';
  
  formCode = '';
  formName = '';
  formDescription = '';
  formPrice: number | null = null;
  
  loading = false;
  saving = false;
  errorMessage = '';
  successMessage = '';
  
  private apiUrl = environment.apiUrl;

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    // Check if user is admin
    const role = this.authService.getCurrentUserRole();
    if (role !== 'Admin') {
      this.router.navigate(['/dashboard/courses']);
      return;
    }

    // Check if we're in edit mode
    this.route.queryParams.subscribe(params => {
      if (params['moduleCode']) {
        this.isEditMode = true;
        this.editingModuleCode = params['moduleCode'];
        this.loadModule();
      }
    });
  }

  loadModule() {
    this.loading = true;
    this.http.get<Module[]>(`${this.apiUrl}/Modules`).subscribe({
      next: (modules) => {
        const module = modules.find(m => m.module_Code === this.editingModuleCode);
        if (module) {
          this.formCode = module.module_Code;
          this.formName = module.module_Name;
          this.formDescription = module.module_Description;
          this.formPrice = module.module_Price;
        } else {
          this.errorMessage = 'Module not found.';
          setTimeout(() => this.goBack(), 2000);
        }
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load module details.';
        this.loading = false;
      }
    });
  }

  saveModule() {
    if (!this.formCode || !this.formName || !this.formDescription || this.formPrice === null) {
      this.errorMessage = 'Please fill in all fields.';
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    const payload = {
      module_Code: this.formCode,
      module_Name: this.formName,
      module_Description: this.formDescription,
      module_Price: this.formPrice
    };

    if (this.isEditMode) {
      // Update existing module
      this.http.put(`${this.apiUrl}/Modules/${this.editingModuleCode}`, payload).subscribe({
        next: () => {
          this.successMessage = 'Module updated successfully!';
          setTimeout(() => this.goBack(), 1500);
        },
        error: (err) => {
          this.errorMessage = err.error || 'Failed to update module.';
          this.saving = false;
        }
      });
    } else {
      // Create new module
      this.http.post(`${this.apiUrl}/Modules`, payload).subscribe({
        next: () => {
          this.successMessage = 'Module created successfully!';
          setTimeout(() => this.goBack(), 1500);
        },
        error: (err) => {
          this.errorMessage = err.error || 'Failed to create module.';
          this.saving = false;
        }
      });
    }
  }

  goBack() {
    this.router.navigate(['/dashboard/courses']);
  }
}
