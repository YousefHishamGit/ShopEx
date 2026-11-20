import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Warehouse, StockBalance, StockMovement, StockAdjustmentRequest } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class StockService {
  private readonly baseUrl = 'https://localhost:7122/api';

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

  getBalances(warehouseId: number, lowStockOnly: boolean = false): Observable<StockBalance[]> {
    const params = new HttpParams()
      .set('warehouseId', warehouseId.toString())
      .set('lowStockOnly', lowStockOnly.toString());
    return this.http.get<StockBalance[]>(`${this.baseUrl}/Stock/balances`, { params });
  }

  getItemBalance(warehouseId: number, rmsItemId: number): Observable<StockBalance> {
    return this.http.get<StockBalance>(`${this.baseUrl}/Stock/balances/${warehouseId}/${rmsItemId}`);
  }

  getMovements(warehouseId?: number, rmsItemId?: number, fromDate?: string, toDate?: string): Observable<StockMovement[]> {
    let params = new HttpParams();
    if (warehouseId) params = params.set('warehouseId', warehouseId.toString());
    if (rmsItemId) params = params.set('rmsItemId', rmsItemId.toString());
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    return this.http.get<StockMovement[]>(`${this.baseUrl}/Stock/movements`, { params });
  }

  adjustStock(request: StockAdjustmentRequest): Observable<StockBalance> {
    return this.http.post<StockBalance>(`${this.baseUrl}/Stock/adjust`, request);
  }
}
