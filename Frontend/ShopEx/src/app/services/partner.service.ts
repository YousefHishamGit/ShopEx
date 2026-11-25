import { API_URL } from '../core/api-config';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PartnersReport, SaveBranchPartnersRequest } from '../models/models';

@Injectable({ providedIn: 'root' })
export class PartnerService {
  private readonly baseUrl = API_URL + '/Partners';

  constructor(private http: HttpClient) {}

  /** @param fromDate / toDate yyyy-MM-dd (local days, both inclusive). Omit for "this month until today". */
  getReport(fromDate?: string, toDate?: string): Observable<PartnersReport> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<PartnersReport>(`${this.baseUrl}/report`, { params });
  }

  /** فتح/إغلاق كارت الإدارة العامة (HQ) */
  setHqEnabled(isEnabled: boolean): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/hq/enabled`, { isEnabled });
  }

  saveBranchPartners(storeId: number, req: SaveBranchPartnersRequest): Observable<unknown> {
    return this.http.put(`${this.baseUrl}/branches/${storeId}`, req);
  }
}
