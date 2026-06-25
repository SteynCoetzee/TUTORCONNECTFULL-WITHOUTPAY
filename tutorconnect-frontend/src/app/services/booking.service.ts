import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Booking, BookingCreate, BookingSlot } from '../models/models';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private slotsUrl = `${environment.apiUrl}/BookingSlots`;
  private bookingsUrl = `${environment.apiUrl}/Bookings`;

  constructor(private http: HttpClient) {}

  getSlots(): Observable<BookingSlot[]> {
    return this.http.get<BookingSlot[]>(this.slotsUrl);
  }

  getAvailableSlots(): Observable<BookingSlot[]> {
    return this.http.get<BookingSlot[]>(`${this.slotsUrl}/available`);
  }

  getBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>(this.bookingsUrl);
  }

  bookSession(data: BookingCreate): Observable<string> {
    return this.http.post(this.bookingsUrl, data, { responseType: 'text' });
  }
}
