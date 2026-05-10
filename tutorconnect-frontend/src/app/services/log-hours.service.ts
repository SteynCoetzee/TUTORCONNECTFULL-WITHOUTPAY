import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LogHours {
  log_Hours_ID: number;
  tutor_ID: number;
  student_ID: number;
  module_Code: string;
  hours_Logged: number;
  log_Date: string;
  description?: string;
}

@Injectable({
  providedIn: 'root'
})
export class LogHoursService {
  private apiUrl = `${environment.apiUrl}/LogHours`;

  constructor(private http: HttpClient) {}

  // Get all logged hours
  getLoggedHours(): Observable<LogHours[]> {
    return this.http.get<LogHours[]>(this.apiUrl);
  }

  // Get logged hours for tutor
  getTutorHours(tutorId: number): Observable<LogHours[]> {
    return this.http.get<LogHours[]>(`${this.apiUrl}/tutor/${tutorId}`);
  }

  // Get logged hours for student
  getStudentHours(studentId: number): Observable<LogHours[]> {
    return this.http.get<LogHours[]>(`${this.apiUrl}/student/${studentId}`);
  }

  // Log hours (tutor only)
  logHours(payload: any): Observable<any> {
    return this.http.post(this.apiUrl, payload);
  }

  // Update logged hours
  updateLoggedHours(id: number, payload: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, payload);
  }

  // Delete logged hours
  deleteLoggedHours(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  // Get total hours for tutor
  getTutorTotalHours(tutorId: number): Observable<{ totalHours: number }> {
    return this.http.get<{ totalHours: number }>(`${this.apiUrl}/tutor/${tutorId}/total`);
  }
}
