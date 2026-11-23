import { API_URL } from '../core/api-config';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ExpenseCategoryDto,
  CreateExpenseCategoryRequest,
  UpdateExpenseCategoryRequest,
  CreateExpenseRequest,
  CreateBranchExpenseRequest,
  UpdateExpenseRequest,
  ExpenseResponse,
  ExpenseSummaryDto,
  ExpenseFilters
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private base = API_URL + '/Expenses';

  constructor(private http: HttpClient) {}

  getStores(): Observable<{ id: number; name: string }[]> {
    return this.http.get<{ id: number; name: string }[]>(`${this.base}/stores`);
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

  toggleCategory(id: number): Observable<{ isActive: boolean }> {
    return this.http.patch<{ isActive: boolean }>(`${this.base}/categories/${id}/toggle-status`, {});
  }

  private toParams(filters: ExpenseFilters, extra: Record<string, string> = {}): HttpParams {
    let params = new HttpParams();
    if (filters.storeId) params = params.set('storeId', filters.storeId.toString());
    if (filters.categoryId) params = params.set('categoryId', filters.categoryId.toString());
    if (filters.fromDate) params = params.set('fromDate', filters.fromDate);
    if (filters.toDate) params = params.set('toDate', filters.toDate);
    if (filters.paymentMethod) params = params.set('paymentMethod', filters.paymentMethod.toString());
    if (filters.search?.trim()) params = params.set('search', filters.search.trim());
    for (const [k, v] of Object.entries(extra)) params = params.set(k, v);
    return params;
  }

  getAll(filters: ExpenseFilters): Observable<ExpenseResponse[]> {
    return this.http.get<ExpenseResponse[]>(this.base, { params: this.toParams(filters) });
  }

  /** الإجماليات والتوزيع على كل النتائج + صفحة واحدة من الحركات */
  getSummary(filters: ExpenseFilters, page = 1, pageSize = 25): Observable<ExpenseSummaryDto> {
    const params = this.toParams(filters, { page: page.toString(), pageSize: pageSize.toString() });
    return this.http.get<ExpenseSummaryDto>(`${this.base}/summary`, { params });
  }

  // ----- موظف الفرع: مصروفات فرعه فقط (الفرع والاسم والتاريخ يحددها السيرفر) -----
  getBranchCategories(): Observable<{ id: number; name: string }[]> {
    return this.http.get<{ id: number; name: string }[]>(`${this.base}/branch/categories`);
  }

  getBranchExpenses(page = 1, pageSize = 25, fromDate?: string, toDate?: string): Observable<ExpenseSummaryDto> {
    let params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ExpenseSummaryDto>(`${this.base}/branch`, { params });
  }

  createBranchExpense(req: CreateBranchExpenseRequest): Observable<ExpenseResponse> {
    return this.http.post<ExpenseResponse>(`${this.base}/branch`, req);
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

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
