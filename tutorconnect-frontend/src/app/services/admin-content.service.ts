import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AdminContent {
  content_ID: number;
  content_Title: string;
  content_Body: string;
  content_Type: string;
  created_Date: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminContentService {
  private apiUrl = `${environment.apiUrl}/AdminContent`;

  constructor(private http: HttpClient) {}

  // Get all admin content
  getContent(): Observable<AdminContent[]> {
    return this.http.get<AdminContent[]>(this.apiUrl);
  }

  // Get content by type
  getContentByType(type: string): Observable<AdminContent[]> {
    return this.http.get<AdminContent[]>(`${this.apiUrl}/type/${type}`);
  }

  // Get specific content
  getContent$(id: number): Observable<AdminContent> {
    return this.http.get<AdminContent>(`${this.apiUrl}/${id}`);
  }

  // Create content
  createContent(payload: any): Observable<any> {
    return this.http.post(this.apiUrl, payload);
  }

  // Update content
  updateContent(id: number, payload: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, payload);
  }

  // Delete content
  deleteContent(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
