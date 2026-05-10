import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Enrollment {
  enrollment_ID: number;
  student_ID: number;
  module_Code: string;
  module_Name: string;
  enrollment_Date: string;
  isActive: boolean;
}

export interface EnrollmentCreateDto {
  student_ID: number;
  module_Code: string;
}

export interface EnrollmentUnenrollDto {
  unenroll_Reason?: string;
}

@Injectable({
  providedIn: 'root'
})
export class EnrollmentService {
  private apiUrl = `${environment.apiUrl}/Enrollment`;

  constructor(private http: HttpClient) {}

  // Enroll student in module
  enrollInModule(payload: EnrollmentCreateDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/enroll`, payload);
  }

  // Unenroll student from module
  unenrollFromModule(studentId: number, moduleCode: string, reason?: string): Observable<any> {
    const payload: EnrollmentUnenrollDto = { unenroll_Reason: reason };
    return this.http.delete(`${this.apiUrl}/unenroll/${moduleCode}?studentId=${studentId}`, { body: payload });
  }

  // Get student's enrolled modules
  getStudentModules(studentId: number): Observable<Enrollment[]> {
    return this.http.get<Enrollment[]>(`${this.apiUrl}/student/${studentId}`);
  }

  // Get module's enrolled students (admin/tutor only)
  getModuleStudents(moduleCode: string): Observable<Enrollment[]> {
    return this.http.get<Enrollment[]>(`${this.apiUrl}/module/${moduleCode}`);
  }

  // Check if student is enrolled in module
  checkEnrollment(studentId: number, moduleCode: string): Observable<{ isEnrolled: boolean }> {
    return this.http.get<{ isEnrolled: boolean }>(`${this.apiUrl}/check/${studentId}/${moduleCode}`);
  }
}
