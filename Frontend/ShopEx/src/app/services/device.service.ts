import { API_URL } from '../core/api-config';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateDeviceRequest,
  DeviceFilters,
  DeviceResponse,
  DeviceSummaryDto,
  SellDeviceRequest,
  UpdateDeviceRequest
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class DeviceService {
  private base = API_URL + '/Devices';

  constructor(private http: HttpClient) {}

  getStores(): Observable<{ id: number; name: string }[]> {
    return this.http.get<{ id: number; name: string }[]>(`${this.base}/stores`);
  }

  getAll(filters: DeviceFilters, page = 1, pageSize = 25): Observable<DeviceSummaryDto> {
    let params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    if (filters.storeId !== undefined && filters.storeId !== null) params = params.set('storeId', filters.storeId.toString());
    if (filters.status) params = params.set('status', filters.status.toString());
    if (filters.fromDate) params = params.set('fromDate', filters.fromDate);
    if (filters.toDate) params = params.set('toDate', filters.toDate);
    if (filters.search?.trim()) params = params.set('search', filters.search.trim());
    return this.http.get<DeviceSummaryDto>(this.base, { params });
  }

  create(req: CreateDeviceRequest): Observable<DeviceResponse> {
    return this.http.post<DeviceResponse>(this.base, req);
  }

  update(id: number, req: UpdateDeviceRequest): Observable<DeviceResponse> {
    return this.http.put<DeviceResponse>(`${this.base}/${id}`, req);
  }

  sell(id: number, req: SellDeviceRequest): Observable<DeviceResponse> {
    return this.http.post<DeviceResponse>(`${this.base}/${id}/sell`, req);
  }

  cancelSale(id: number): Observable<DeviceResponse> {
    return this.http.post<DeviceResponse>(`${this.base}/${id}/cancel-sale`, {});
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
