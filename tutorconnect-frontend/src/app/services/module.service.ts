import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Module } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ModuleService {
  private apiUrl = `${environment.apiUrl}/Modules`;

  constructor(private http: HttpClient) {}

  getModules(): Observable<Module[]> {
    return this.http.get<Module[]>(this.apiUrl);
  }

  createModule(data: Module): Observable<string> {
    return this.http.post(this.apiUrl, data, { responseType: 'text' });
  }
}
