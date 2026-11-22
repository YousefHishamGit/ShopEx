import { API_URL } from '../core/api-config';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  PagedResponse,
  CustomerSummary,
  CustomerOpenInvoice,
  CustomerStatement,
  CustomerInvoiceDetail
} from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class CustomerStatementService {
  private readonly baseUrl = API_URL;

  constructor(private http: HttpClient) {}

  getCustomers(
    search?: string,
    minDaysOld?: number,
    page: number = 1,
    pageSize: number = 20
  ): Observable<PagedResponse<CustomerSummary>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search && search.trim()) {
      params = params.set('search', search.trim());
    }
    if (minDaysOld !== undefined && minDaysOld !== null && minDaysOld > 0) {
      params = params.set('minDaysOld', minDaysOld.toString());
    }

    return this.http.get<PagedResponse<CustomerSummary>>(`${this.baseUrl}/CustomerStatements/customers`, { params });
  }

  getOpenInvoices(
    customerId?: number,
    storeId?: number,
    minDaysOld?: number,
    search?: string,
    page: number = 1,
    pageSize: number = 20
  ): Observable<PagedResponse<CustomerOpenInvoice>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (customerId) params = params.set('customerId', customerId.toString());
    if (storeId) params = params.set('storeId', storeId.toString());
    if (minDaysOld !== undefined && minDaysOld !== null && minDaysOld > 0) {
      params = params.set('minDaysOld', minDaysOld.toString());
    }
    if (search && search.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http.get<PagedResponse<CustomerOpenInvoice>>(`${this.baseUrl}/CustomerStatements/open-invoices`, { params });
  }

  getCustomerStatement(customerId: number, minDaysOld?: number): Observable<CustomerStatement> {
    let params = new HttpParams();
    if (minDaysOld !== undefined && minDaysOld !== null && minDaysOld > 0) {
      params = params.set('minDaysOld', minDaysOld.toString());
    }
    return this.http.get<CustomerStatement>(`${this.baseUrl}/CustomerStatements/customers/${customerId}`, { params });
  }

  getCustomerStatementByAccount(accountNumber: string, minDaysOld?: number): Observable<CustomerStatement> {
    let params = new HttpParams();
    if (minDaysOld !== undefined && minDaysOld !== null && minDaysOld > 0) {
      params = params.set('minDaysOld', minDaysOld.toString());
    }
    return this.http.get<CustomerStatement>(`${this.baseUrl}/CustomerStatements/by-account/${encodeURIComponent(accountNumber.trim())}`, { params });
  }

  getInvoiceDetail(orderId: number): Observable<CustomerInvoiceDetail> {
    return this.http.get<CustomerInvoiceDetail>(`${this.baseUrl}/CustomerStatements/invoices/${orderId}`);
  }
}
