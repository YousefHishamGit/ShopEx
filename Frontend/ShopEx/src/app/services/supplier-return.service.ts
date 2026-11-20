import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  SupplierReturn,
  SupplierReturnSummary,
  CreateSupplierReturnRequest,
  AddSupplierRefundRequest
} from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class SupplierReturnService {
  private readonly baseUrl = 'https://localhost:7122/api';

  constructor(private http: HttpClient) {}

  getReturns(supplierId?: number, status?: number, warehouseId?: number): Observable<SupplierReturnSummary[]> {
    let params = new HttpParams();
    if (supplierId) params = params.set('supplierId', supplierId.toString());
    if (status) params = params.set('status', status.toString());
    if (warehouseId) params = params.set('warehouseId', warehouseId.toString());

    return this.http.get<SupplierReturnSummary[]>(`${this.baseUrl}/SupplierReturns`, { params });
  }

  getReturnById(id: number): Observable<SupplierReturn> {
    return this.http.get<SupplierReturn>(`${this.baseUrl}/SupplierReturns/${id}`);
  }

  getReturnByNumber(returnNumber: string): Observable<SupplierReturn> {
    return this.http.get<SupplierReturn>(`${this.baseUrl}/SupplierReturns/by-number/${returnNumber}`);
  }

  createReturn(request: CreateSupplierReturnRequest): Observable<SupplierReturn> {
    return this.http.post<SupplierReturn>(`${this.baseUrl}/SupplierReturns`, request);
  }

  confirmReturn(id: number): Observable<SupplierReturn> {
    return this.http.post<SupplierReturn>(`${this.baseUrl}/SupplierReturns/${id}/confirm`, {});
  }

  cancelReturn(id: number): Observable<SupplierReturn> {
    return this.http.post<SupplierReturn>(`${this.baseUrl}/SupplierReturns/${id}/cancel`, {});
  }

  addRefund(id: number, request: AddSupplierRefundRequest): Observable<SupplierReturn> {
    return this.http.post<SupplierReturn>(`${this.baseUrl}/SupplierReturns/${id}/refunds`, request);
  }
}
