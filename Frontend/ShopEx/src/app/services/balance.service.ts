import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  BalanceResponse,
  BalanceSummaryResponse,
  BalanceClosureResponse,
  ChargeBalanceRequest,
  CloseBalanceRequest,
  CloseStoreBalanceRequest,
  StoreBalanceGroup,
  BalanceTransactionItem,
  StoreOption
} from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class BalanceService {
  private readonly baseUrl = 'https://localhost:7122/api/Balances';

  constructor(private http: HttpClient) {}

  /**
   * استرجاع قائمة الفروع المسجلة في RMS
   */
  getStores(): Observable<StoreOption[]> {
    return this.http.get<StoreOption[]>(`${this.baseUrl}/stores`);
  }

  /**
   * استرجاع شحنات الأرصدة مع إمكانية الفلترة
   */
  getAll(
    storeId?: number,
    balanceType?: number,
    fromDate?: string,
    toDate?: string
  ): Observable<BalanceSummaryResponse[]> {
    let params = new HttpParams();

    if (storeId !== undefined && storeId !== null && storeId > 0) {
      params = params.set('storeId', storeId.toString());
    }
    if (balanceType !== undefined && balanceType !== null && balanceType > 0) {
      params = params.set('balanceType', balanceType.toString());
    }
    if (fromDate) {
      params = params.set('fromDate', fromDate);
    }
    if (toDate) {
      params = params.set('toDate', toDate);
    }

    return this.http.get<BalanceSummaryResponse[]>(this.baseUrl, { params });
  }

  /**
   * استرجاع تفاصيل شحنة معينة بالـ ID
   */
  getById(id: number): Observable<BalanceResponse> {
    return this.http.get<BalanceResponse>(`${this.baseUrl}/${id}`);
  }

  /**
   * شحن رصيد جديد لفرع معين
   */
  charge(request: ChargeBalanceRequest): Observable<BalanceResponse> {
    return this.http.post<BalanceResponse>(`${this.baseUrl}/charge`, request);
  }

  /**
   * تقفيل رصيد يومي (تسجيل ما استخدمه الفرع فعلياً)
   */
  close(balanceId: number, request: CloseBalanceRequest): Observable<BalanceClosureResponse> {
    return this.http.post<BalanceClosureResponse>(`${this.baseUrl}/${balanceId}/closures`, request);
  }

  /**
   * استرجاع سجل التقفيلات لشحنة معينة
   */
  getClosures(balanceId: number): Observable<BalanceClosureResponse[]> {
    return this.http.get<BalanceClosureResponse[]>(`${this.baseUrl}/${balanceId}/closures`);
  }

  /**
   * استرجاع الأرصدة التراكمية المجمعة لكل فرع ولكل وسيلة دفع (سطر واحد يجمع الشحنات والتقفيلات)
   */
  getGrouped(storeId?: number, balanceType?: number): Observable<StoreBalanceGroup[]> {
    let params = new HttpParams();
    if (storeId !== undefined && storeId !== null && storeId > 0) {
      params = params.set('storeId', storeId.toString());
    }
    if (balanceType !== undefined && balanceType !== null && balanceType > 0) {
      params = params.set('balanceType', balanceType.toString());
    }
    return this.http.get<StoreBalanceGroup[]>(`${this.baseUrl}/grouped`, { params });
  }

  /**
   * تقفيل رصيد على مستوى الفرع ونوع الوسيلة مباشرة
   */
  closeStore(request: CloseStoreBalanceRequest): Observable<BalanceClosureResponse[]> {
    return this.http.post<BalanceClosureResponse[]>(`${this.baseUrl}/close-store`, request);
  }

  /**
   * استرجاع كشف الحركات والتحويلات التفصيلي بالترتيب الزمني
   */
  getLedger(
    storeId?: number,
    balanceType?: number,
    fromDate?: string,
    toDate?: string
  ): Observable<BalanceTransactionItem[]> {
    let params = new HttpParams();
    if (storeId !== undefined && storeId !== null && storeId > 0) {
      params = params.set('storeId', storeId.toString());
    }
    if (balanceType !== undefined && balanceType !== null && balanceType > 0) {
      params = params.set('balanceType', balanceType.toString());
    }
    if (fromDate) {
      params = params.set('fromDate', fromDate);
    }
    if (toDate) {
      params = params.set('toDate', toDate);
    }
    return this.http.get<BalanceTransactionItem[]>(`${this.baseUrl}/ledger`, { params });
  }
}
