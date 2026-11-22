import { API_URL } from '../core/api-config';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardData } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private readonly baseUrl = API_URL + '/Dashboard';

  constructor(private http: HttpClient) {}

  /** @param date yyyy-MM-dd (local day). Omit for today. */
  get(date?: string): Observable<DashboardData> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<DashboardData>(this.baseUrl, { params });
  }
}
