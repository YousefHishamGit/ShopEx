import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ExpenseCategoryDto,
  CreateExpenseCategoryRequest,
  UpdateExpenseCategoryRequest,
  CreateExpenseRequest,
  UpdateExpenseRequest,
  ExpenseResponse,
  ExpenseSummaryDto,
  ExpensePaymentMethod
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private base = 'https://localhost:7122/api/Expenses';

  constructor(private http: HttpClient) {}

  getStores(): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/stores`);
  }

  getCategories(includeInactive = false): Observable<ExpenseCategoryDto[]> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ExpenseCategoryDto[]>(`${this.base}/categories`, { params });
  }

  createCategory(req: CreateExpenseCategoryRequest): Observable<ExpenseCategoryDto> {
    return this.http.post<ExpenseCategoryDto>(`${this.base}/categories`, req);
  }

  updateCategory(id: number, req: UpdateExpenseCategoryRequest): Observable<ExpenseCategoryDto> {
    return this.http.put<ExpenseCategoryDto>(`${this.base}/categories/${id}`, req);
  }

  toggleCategory(id: number): Observable<any> {
    return this.http.patch(`${this.base}/categories/${id}/toggle-status`, {});
  }

  getAll(filters: {
    storeId?: number;
    categoryId?: number;
    fromDate?: string;
    toDate?: string;
    paymentMethod?: number;
  }): Observable<ExpenseResponse[]> {
    let params = new HttpParams();
    if (filters.storeId) params = params.set('storeId', filters.storeId.toString());
    if (filters.categoryId) params = params.set('categoryId', filters.categoryId.toString());
    if (filters.fromDate) params = params.set('fromDate', filters.fromDate);
    if (filters.toDate) params = params.set('toDate', filters.toDate);
    if (filters.paymentMethod) params = params.set('paymentMethod', filters.paymentMethod.toString());
    return this.http.get<ExpenseResponse[]>(this.base, { params });
  }

  getSummary(filters: {
    storeId?: number;
    categoryId?: number;
    fromDate?: string;
    toDate?: string;
    paymentMethod?: number;
  }): Observable<ExpenseSummaryDto> {
    let params = new HttpParams();
    if (filters.storeId) params = params.set('storeId', filters.storeId.toString());
    if (filters.categoryId) params = params.set('categoryId', filters.categoryId.toString());
    if (filters.fromDate) params = params.set('fromDate', filters.fromDate);
    if (filters.toDate) params = params.set('toDate', filters.toDate);
    if (filters.paymentMethod) params = params.set('paymentMethod', filters.paymentMethod.toString());
    return this.http.get<ExpenseSummaryDto>(`${this.base}/summary`, { params });
  }

  getById(id: number): Observable<ExpenseResponse> {
    return this.http.get<ExpenseResponse>(`${this.base}/${id}`);
  }

  create(req: CreateExpenseRequest): Observable<ExpenseResponse> {
    return this.http.post<ExpenseResponse>(this.base, req);
  }

  update(id: number, req: UpdateExpenseRequest): Observable<ExpenseResponse> {
    return this.http.put<ExpenseResponse>(`${this.base}/${id}`, req);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.base}/${id}`);
  }
}
