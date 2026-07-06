import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface BusinessRule {
  rule_ID: number;
  rule_Name: string;
  rule_Value: number;
  description: string;
}

const RULE_META: Record<string, { label: string; unit: string; min: number; max: number; icon: string; hint: string }> = {
  session_timeout_minutes: {
    label: 'AFK Session Timeout',
    unit: 'minutes',
    min: 1,
    max: 480,
    icon: 'timer_off',
    hint: 'Users will be automatically logged out after this many minutes of inactivity (no mouse or keyboard activity).'
  }
};

@Component({
  selector: 'app-business-logic',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './business-logic.component.html',
  styleUrl: './business-logic.component.css'
})
export class BusinessLogicComponent implements OnInit {
  rules: BusinessRule[] = [];
  editValues: Record<number, number> = {};
  saving: Record<number, boolean> = {};
  saved: Record<number, boolean> = {};
  loading = false;
  errorMessage = '';

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadRules();
  }

  loadRules() {
    this.loading = true;
    this.http.get<BusinessRule[]>(`${this.apiUrl}/BusinessRules`).subscribe({
      next: (data) => {
        this.rules = data;
        this.editValues = {};
        data.forEach(r => { this.editValues[r.rule_ID] = r.rule_Value; });
        this.loading = false;
      },
      error: () => { this.errorMessage = 'Failed to load settings.'; this.loading = false; }
    });
  }

  getMeta(name: string) {
    return RULE_META[name] ?? { label: name, unit: '', min: 0, max: 9999, icon: 'settings', hint: '' };
  }

  saveRule(rule: BusinessRule) {
    const val = this.editValues[rule.rule_ID];
    const meta = this.getMeta(rule.rule_Name);
    if (val < meta.min || val > meta.max) {
      this.errorMessage = `Value for "${meta.label}" must be between ${meta.min} and ${meta.max}.`;
      return;
    }
    this.saving[rule.rule_ID] = true;
    this.errorMessage = '';
    this.http.put(`${this.apiUrl}/BusinessRules/${rule.rule_ID}`, { rule_Value: val }).subscribe({
      next: () => {
        this.saving[rule.rule_ID] = false;
        this.saved[rule.rule_ID] = true;
        rule.rule_Value = val;
        setTimeout(() => { this.saved[rule.rule_ID] = false; }, 2500);
      },
      error: () => {
        this.saving[rule.rule_ID] = false;
        this.errorMessage = 'Failed to save. Please try again.';
      }
    });
  }

  isDirty(rule: BusinessRule): boolean {
    return this.editValues[rule.rule_ID] !== rule.rule_Value;
  }
}
