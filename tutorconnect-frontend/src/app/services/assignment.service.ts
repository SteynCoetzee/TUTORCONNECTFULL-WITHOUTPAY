import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Assignment {
  assignment_ID: number;
  assignment_Name: string;
  assignment_Date: string;
  module_Code: string;
}

export interface AssignmentSubmission {
  submission_ID: number;
  assignment_ID: number;
  student_ID: number;
  file_Name: string;
  submission_Date: string;
  grade?: number;
  feedback?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AssignmentService {
  private apiUrl = `${environment.apiUrl}/Assignments`;

  constructor(private http: HttpClient) {}

  // Get all assignments
  getAssignments(): Observable<Assignment[]> {
    return this.http.get<Assignment[]>(this.apiUrl);
  }

  // Get specific assignment
  getAssignment(id: number): Observable<Assignment> {
    return this.http.get<Assignment>(`${this.apiUrl}/${id}`);
  }

  // Get assignments for a module
  getModuleAssignments(moduleCode: string): Observable<Assignment[]> {
    return this.http.get<Assignment[]>(`${this.apiUrl}/module/${moduleCode}`);
  }

  // Create assignment (admin/tutor only)
  createAssignment(payload: any): Observable<any> {
    return this.http.post(this.apiUrl, payload);
  }

  // Update assignment (admin/tutor only)
  updateAssignment(id: number, payload: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, payload);
  }

  // Delete assignment (admin/tutor only)
  deleteAssignment(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  // Submit assignment with file
  submitAssignment(assignmentId: number, studentId: number, file: File): Observable<any> {
    const formData = new FormData();
    formData.append('studentId', studentId.toString());
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/${assignmentId}/submit`, formData);
  }

  // Get submissions for assignment
  getSubmissions(assignmentId: number): Observable<AssignmentSubmission[]> {
    return this.http.get<AssignmentSubmission[]>(`${this.apiUrl}/${assignmentId}/submissions`);
  }

  // Grade a submission
  gradeSubmission(assignmentId: number, submissionId: number, grade: number, feedback?: string): Observable<any> {
    const payload = { grade, feedback };
    return this.http.put(`${this.apiUrl}/${assignmentId}/submissions/${submissionId}/grade`, payload);
  }
}
