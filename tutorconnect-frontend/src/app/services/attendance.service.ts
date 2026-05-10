import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Attendance {
  session_Attendance_ID: number;
  session_ID: number;
  student_ID: number;
  attendance_Status_ID: number;
  status_Name: string;
}

export interface AttendanceStatus {
  attendance_Status_ID: number;
  status_Name: string;
}

@Injectable({
  providedIn: 'root'
})
export class AttendanceService {
  private apiUrl = `${environment.apiUrl}/Attendance`;

  constructor(private http: HttpClient) {}

  // Get attendance records for session
  getSessionAttendance(sessionId: number): Observable<Attendance[]> {
    return this.http.get<Attendance[]>(`${this.apiUrl}/session/${sessionId}`);
  }

  // Get attendance records for student
  getStudentAttendance(studentId: number): Observable<Attendance[]> {
    return this.http.get<Attendance[]>(`${this.apiUrl}/student/${studentId}`);
  }

  // Record attendance
  recordAttendance(sessionId: number, studentId: number, statusId: number): Observable<any> {
    return this.http.post(this.apiUrl, { session_ID: sessionId, student_ID: studentId, attendance_Status_ID: statusId });
  }

  // Get attendance statuses
  getStatuses(): Observable<AttendanceStatus[]> {
    return this.http.get<AttendanceStatus[]>(`${this.apiUrl}/statuses`);
  }

  // Get attendance summary
  getAttendanceSummary(studentId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/summary/${studentId}`);
  }
}
