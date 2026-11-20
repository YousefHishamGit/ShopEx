import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Dealer,
  CreateDealerRequest,
  UpdateDealerRequest,
  Consignment,
  ConsignmentSummary,
  CreateConsignmentRequest,
  CreateConsignmentSettlementRequest,
  AddConsignmentPaymentRequest
} from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class ConsignmentService {
  private readonly baseUrl = 'https://localhost:7122/api';

  constructor(private http: HttpClient) {}

  // ----------------------------------------------------
  // Dealers (البنزينات / المحلات)
  // ----------------------------------------------------
  getDealers(activeOnly?: boolean, search?: string): Observable<Dealer[]> {
    let params = new HttpParams();
    if (activeOnly !== undefined && activeOnly !== null) {
      params = params.set('activeOnly', activeOnly.toString());
    }
    if (search && search.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<Dealer[]>(`${this.baseUrl}/Dealers`, { params });
  }

  getDealer(id: number): Observable<Dealer> {
    return this.http.get<Dealer>(`${this.baseUrl}/Dealers/${id}`);
  }

  createDealer(request: CreateDealerRequest): Observable<Dealer> {
    return this.http.post<Dealer>(`${this.baseUrl}/Dealers`, request);
  }

  updateDealer(id: number, request: UpdateDealerRequest): Observable<Dealer> {
    return this.http.put<Dealer>(`${this.baseUrl}/Dealers/${id}`, request);
  }

  deleteDealer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/Dealers/${id}`);
  }

  // ----------------------------------------------------
  // Consignments (أذونات البنزيمات / بضاعة أمانة)
  // ----------------------------------------------------
  getConsignments(
    dealerId?: number,
    status?: string,
    fromDate?: string,
    toDate?: string
  ): Observable<ConsignmentSummary[]> {
    let params = new HttpParams();
    if (dealerId) params = params.set('dealerId', dealerId.toString());
    if (status && status !== 'ALL') params = params.set('status', status);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    return this.http.get<ConsignmentSummary[]>(`${this.baseUrl}/Consignments`, { params });
  }

  getConsignment(id: number): Observable<Consignment> {
    return this.http.get<Consignment>(`${this.baseUrl}/Consignments/${id}`);
  }

  getConsignmentByNumber(consignmentNumber: string): Observable<Consignment> {
    return this.http.get<Consignment>(`${this.baseUrl}/Consignments/by-number/${consignmentNumber}`);
  }

  createConsignment(request: CreateConsignmentRequest): Observable<Consignment> {
    return this.http.post<Consignment>(`${this.baseUrl}/Consignments`, request);
  }

  addSettlement(id: number, request: CreateConsignmentSettlementRequest): Observable<Consignment> {
    return this.http.post<Consignment>(`${this.baseUrl}/Consignments/${id}/settle`, request);
  }

  addPayment(id: number, request: AddConsignmentPaymentRequest): Observable<Consignment> {
    return this.http.post<Consignment>(`${this.baseUrl}/Consignments/${id}/payments`, request);
  }

  cancelConsignment(id: number): Observable<Consignment> {
    return this.http.post<Consignment>(`${this.baseUrl}/Consignments/${id}/cancel`, {});
  }
}
