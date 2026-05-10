import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ModuleResource {
  module_Resource_ID: number;
  module_Resource_Name: string;
  module_Resource_Type_ID: string;
  module_Code: string;
}

@Injectable({
  providedIn: 'root'
})
export class ModuleResourceService {
  private apiUrl = `${environment.apiUrl}/ModuleResources`;

  constructor(private http: HttpClient) {}

  // Get all resources
  getResources(): Observable<ModuleResource[]> {
    return this.http.get<ModuleResource[]>(this.apiUrl);
  }

  // Get resources for module
  getModuleResources(moduleCode: string): Observable<ModuleResource[]> {
    return this.http.get<ModuleResource[]>(`${this.apiUrl}/module/${moduleCode}`);
  }

  // Get specific resource
  getResource(id: number): Observable<ModuleResource> {
    return this.http.get<ModuleResource>(`${this.apiUrl}/${id}`);
  }

  // Create resource (tutor only)
  createResource(payload: any): Observable<any> {
    return this.http.post(this.apiUrl, payload);
  }

  // Update resource
  updateResource(id: number, payload: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, payload);
  }

  // Delete resource
  deleteResource(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
