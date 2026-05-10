import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Grade {
  grade_ID: number;
  assessment_Name: string;
  assessment_Type: string; // 'Quiz' or 'Assignment'
  score: number;
  total_Points: number;
  percentage: number;
  feedback?: string;
  grade_Date: string;
}

export interface AverageGrade {
  studentId: number;
  averageGrade: number;
  totalAssessments: number;
  quizCount: number;
  assignmentCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class GradeService {
  private apiUrl = `${environment.apiUrl}/Grades`;

  constructor(private http: HttpClient) {}

  // Get all grades for a student
  getStudentGrades(studentId: number): Observable<Grade[]> {
    return this.http.get<Grade[]>(`${this.apiUrl}/student/${studentId}`);
  }

  // Get grades for a student in a specific module
  getStudentModuleGrades(studentId: number, moduleCode: string): Observable<Grade[]> {
    return this.http.get<Grade[]>(`${this.apiUrl}/student/${studentId}/module/${moduleCode}`);
  }

  // Get specific quiz grade
  getQuizGrade(quizId: number, studentId: number): Observable<Grade> {
    return this.http.get<Grade>(`${this.apiUrl}/quiz/${quizId}/student/${studentId}`);
  }

  // Get average grade
  getAverageGrade(studentId: number): Observable<AverageGrade> {
    return this.http.get<AverageGrade>(`${this.apiUrl}/average/student/${studentId}`);
  }
}
