import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LicenseInfo } from '../models/models';
import { AuthService } from './auth.service';

/** Developer-only API for the subscription (license) switch. */
@Injectable({
  providedIn: 'root'
})
export class LicenseService {
  private readonly baseUrl = `${AuthService.apiUrl}/Dev/license`;

  constructor(private http: HttpClient) {}

  get(): Observable<LicenseInfo> {
    return this.http.get<LicenseInfo>(this.baseUrl);
  }

  setEnabled(isEnabled: boolean): Observable<LicenseInfo> {
    return this.http.put<LicenseInfo>(`${this.baseUrl}/enabled`, { isEnabled });
  }

  extend(months: number): Observable<LicenseInfo> {
    return this.http.post<LicenseInfo>(`${this.baseUrl}/extend`, { months });
  }
}
