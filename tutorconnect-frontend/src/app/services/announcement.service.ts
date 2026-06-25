import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Announcement, AnnouncementCreate, AnnouncementUpdate } from '../models/models';

@Injectable({ providedIn: 'root' })
export class AnnouncementService {
  private apiUrl = `${environment.apiUrl}/Announcements`;

  constructor(private http: HttpClient) {}

  getAnnouncements(): Observable<Announcement[]> {
    return this.http.get<Announcement[]>(this.apiUrl);
  }

  getWebsiteAnnouncements(): Observable<Announcement[]> {
    return this.http.get<Announcement[]>(`${this.apiUrl}/website`);
  }

  getModuleAnnouncements(moduleCode: string): Observable<Announcement[]> {
    return this.http.get<Announcement[]>(`${this.apiUrl}/module/${moduleCode}`);
  }

  createAnnouncement(data: AnnouncementCreate): Observable<string> {
    return this.http.post(this.apiUrl, data, { responseType: 'text' });
  }

  updateAnnouncement(id: number, data: AnnouncementUpdate): Observable<string> {
    return this.http.put(`${this.apiUrl}/${id}`, data, { responseType: 'text' });
  }

  deleteAnnouncement(id: number): Observable<string> {
    return this.http.delete(`${this.apiUrl}/${id}`, { responseType: 'text' });
  }
}
