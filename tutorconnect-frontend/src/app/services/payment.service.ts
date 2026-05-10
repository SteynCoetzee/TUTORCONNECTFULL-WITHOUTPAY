import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Payment {
  payment_ID: number;
  student_ID: number;
  amount: number;
  module_Code: string;
  payment_Status: string;
  payment_Date: string;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private apiUrl = `${environment.apiUrl}/Payments`;

  constructor(private http: HttpClient) {}

  // Get all payments
  getPayments(): Observable<Payment[]> {
    return this.http.get<Payment[]>(this.apiUrl);
  }

  // Get payments for a student
  getStudentPayments(studentId: number): Observable<Payment[]> {
    return this.http.get<Payment[]>(`${this.apiUrl}/student/${studentId}`);
  }

  // Record payment
  recordPayment(payload: any): Observable<any> {
    return this.http.post(this.apiUrl, payload);
  }

  // Update payment status (admin only)
  updatePaymentStatus(paymentId: number, status: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${paymentId}`, { payment_Status: status });
  }
}
