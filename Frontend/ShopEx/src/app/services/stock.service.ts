import { API_URL } from '../core/api-config';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Warehouse, StockBalance, PagedStockBalances, StockMovement, StockAdjustmentRequest } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class StockService {
  private readonly baseUrl = API_URL;

  constructor(private http: HttpClient) {}

  getWarehouses(activeOnly: boolean = false): Observable<Warehouse[]> {
    const params = new HttpParams().set('activeOnly', activeOnly.toString());
    return this.http.get<Warehouse[]>(`${this.baseUrl}/Warehouses`, { params });
  }

  getWarehouse(id: number): Observable<Warehouse> {
    return this.http.get<Warehouse>(`${this.baseUrl}/Warehouses/${id}`);
  }

  createWarehouse(request: { name: string; location?: string; isDefault: boolean; isActive: boolean }): Observable<Warehouse> {
    return this.http.post<Warehouse>(`${this.baseUrl}/Warehouses`, request);
  }

  updateWarehouse(id: number, request: { name: string; location?: string; isDefault: boolean; isActive: boolean }): Observable<Warehouse> {
    return this.http.put<Warehouse>(`${this.baseUrl}/Warehouses/${id}`, request);
  }

  /** One page of in-stock rows. Warehouse-wide totals are only computed when includeTotals is true. */
  getBalances(warehouseId: number, page: number = 1, pageSize: number = 20, search: string = '', includeTotals: boolean = true): Observable<PagedStockBalances> {
    let params = new HttpParams()
      .set('warehouseId', warehouseId.toString())
      .set('page', page.toString())
      .set('pageSize', pageSize.toString())
      .set('includeTotals', includeTotals.toString());
    if (search) params = params.set('search', search);
    return this.http.get<PagedStockBalances>(`${this.baseUrl}/Stock/balances`, { params });
  }

  /** All in-stock rows for pickers/lookups, in a single request. */
  getAllBalances(warehouseId: number): Observable<StockBalance[]> {
    const params = new HttpParams().set('warehouseId', warehouseId.toString());
    return this.http.get<StockBalance[]>(`${this.baseUrl}/Stock/balances/all`, { params });
  }

  getItemBalance(warehouseId: number, rmsItemId: number): Observable<StockBalance> {
    return this.http.get<StockBalance>(`${this.baseUrl}/Stock/balances/${warehouseId}/${rmsItemId}`);
  }

  /** Latest 30 movements for the warehouse. */
  getMovements(warehouseId?: number): Observable<StockMovement[]> {
    let params = new HttpParams();
    if (warehouseId) params = params.set('warehouseId', warehouseId.toString());
    return this.http.get<StockMovement[]>(`${this.baseUrl}/Stock/movements`, { params });
  }

  adjustStock(request: StockAdjustmentRequest): Observable<StockBalance> {
    return this.http.post<StockBalance>(`${this.baseUrl}/Stock/adjust`, request);
  }
}
