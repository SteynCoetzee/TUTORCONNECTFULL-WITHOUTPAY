import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Quiz {
  quiz_ID: number;
  quiz_Name: string;
  quiz_Details: string;
  quiz_Date: string;
  start_Time: string;
  end_Time: string;
  module_Code: string;
}

export interface QuizAnswer {
  question_ID: number;
  selected_Answer: string;
}

export interface QuizSubmission {
  student_ID: number;
  answers: QuizAnswer[];
}

export interface QuizResult {
  quizId: number;
  studentId: number;
  score: number;
  submissionDate: string;
}

@Injectable({
  providedIn: 'root'
})
export class QuizService {
  private apiUrl = `${environment.apiUrl}/Quizzes`;

  constructor(private http: HttpClient) {}

  // Get all quizzes
  getQuizzes(): Observable<Quiz[]> {
    return this.http.get<Quiz[]>(this.apiUrl);
  }

  // Get specific quiz
  getQuiz(id: number): Observable<Quiz> {
    return this.http.get<Quiz>(`${this.apiUrl}/${id}`);
  }

  // Get quizzes for a module
  getModuleQuizzes(moduleCode: string): Observable<Quiz[]> {
    return this.http.get<Quiz[]>(`${this.apiUrl}/module/${moduleCode}`);
  }

  // Create quiz (admin/tutor only)
  createQuiz(payload: any): Observable<any> {
    return this.http.post(this.apiUrl, payload);
  }

  // Update quiz (admin/tutor only)
  updateQuiz(id: number, payload: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, payload);
  }

  // Delete quiz (admin/tutor only)
  deleteQuiz(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  // Submit quiz answers
  submitQuiz(quizId: number, submission: QuizSubmission): Observable<any> {
    return this.http.post(`${this.apiUrl}/${quizId}/submit`, submission);
  }

  // Get quiz result
  getQuizResult(quizId: number, studentId: number): Observable<QuizResult> {
    return this.http.get<QuizResult>(`${this.apiUrl}/${quizId}/student/${studentId}/result`);
  }
}
