import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private apiUrl = `${environment.apiUrl}/Reports`;
  constructor(private http: HttpClient) {}

  private params(from?: string, to?: string): HttpParams {
    let p = new HttpParams();
    if (from) p = p.set('from', from);
    if (to)   p = p.set('to', to);
    return p;
  }

  getTutorHoursReport(from?: string, to?: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/tutor-hours`, { params: this.params(from, to) });
  }
  getMonthlyIncome(from?: string, to?: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/monthly-income`, { params: this.params(from, to) });
  }
  getTutorRatingsReport(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/tutor-ratings`);
  }
  getMonthlyStudentsReport(from?: string, to?: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/monthly-students`, { params: this.params(from, to) });
  }
  getSessionsReport(from?: string, to?: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/sessions`, { params: this.params(from, to) });
  }
  getPopularModulesReport(from?: string, to?: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/popular-modules`, { params: this.params(from, to) });
  }
  getCustomReport(entity: string, groupBy: string, from?: string, to?: string): Observable<any[]> {
    let p = new HttpParams().set('entity', entity).set('groupBy', groupBy);
    if (from) p = p.set('from', from);
    if (to)   p = p.set('to', to);
    return this.http.get<any[]>(`${this.apiUrl}/custom`, { params: p });
  }
}
