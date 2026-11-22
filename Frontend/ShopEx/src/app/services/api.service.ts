import { API_URL } from '../core/api-config';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  Supplier,
  SupplierItem,
  SupplierInvoice,
  SupplierInvoicesPage,
  CreateInvoiceRequest,
  AddPaymentRequest,
  Worksheet,
  OpenWorksheetDto,
  OpenWorksheetWithPurchasesRequest,
  PurchaseLineDto,
  PagedResponse,
  SupplierStatement,
  StoreOption
} from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = API_URL;

  constructor(private http: HttpClient) { }

  // Stores / Branches from RMS Database
  getStores(): Observable<StoreOption[]> {
    return this.http.get<any[]>(`${this.baseUrl}/Stores`).pipe(
      map(list => (list || []).map(s => ({
        storeId: s.storeId || s.id,
        id: s.storeId || s.id,
        storeName: s.storeName || s.name || `فرع #${s.storeId || s.id}`,
        name: s.storeName || s.name || `فرع #${s.storeId || s.id}`,
        code: s.code,
        city: s.city,
        address: s.address,
        phone: s.phone
      })))
    );
  }

  // Direct RMS Items Search & Retrieval (with real price & cost from DB)
  searchItems(query: string = '', limit: number = 50): Observable<SupplierItem[]> {
    let params = new HttpParams().set('limit', limit.toString());
    if (query && query.trim()) {
      params = params.set('query', query.trim());
    }
    return this.http.get<any[]>(`${this.baseUrl}/RMSItems/search`, { params }).pipe(
      map(items => (items || []).map(i => ({
        id: i.id || i.ID,
        description: i.description || i.Description,
        itemLookupCode: i.itemLookupCode || i.ItemLookupCode,
        price: i.price ?? i.Price ?? 0,
        cost: i.cost ?? i.Cost ?? 0,
        quantity: i.quantity ?? i.Quantity ?? 0,
        taxRate: 0
      })))
    );
  }

  getItemById(id: number): Observable<SupplierItem> {
    return this.http.get<any>(`${this.baseUrl}/RMSItems/${id}`).pipe(
      map(i => ({
        id: i.id || i.ID,
        description: i.description || i.Description,
        itemLookupCode: i.itemLookupCode || i.ItemLookupCode,
        price: i.price ?? i.Price ?? 0,
        cost: i.cost ?? i.Cost ?? 0,
        quantity: i.quantity ?? i.Quantity ?? 0,
        taxRate: 0
      }))
    );
  }

  getItemByBarcode(barcode: string): Observable<SupplierItem> {
    return this.http.get<any>(`${this.baseUrl}/RMSItems/barcode/${encodeURIComponent(barcode.trim())}`).pipe(
      map(i => ({
        id: i.id || i.ID,
        description: i.description || i.Description,
        itemLookupCode: i.itemLookupCode || i.ItemLookupCode,
        price: i.price ?? i.Price ?? 0,
        cost: i.cost ?? i.Cost ?? 0,
        quantity: i.quantity ?? i.Quantity ?? 0,
        taxRate: 0
      }))
    );
  }

  // Suppliers with server-side pagination & search per page request
  getSuppliers(page: number = 1, pageSize: number = 10, search: string = ''): Observable<PagedResponse<Supplier>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search && search.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http.get<any>(`${this.baseUrl}/RMSSuppliers`, { params }).pipe(
      map(res => {
        // If backend returned array directly
        if (Array.isArray(res)) {
          let items = res;
          if (search && search.trim()) {
            const term = search.trim().toLowerCase();
            items = items.filter(s =>
              (s.supplierName && s.supplierName.toLowerCase().includes(term)) ||
              (s.code && s.code.toLowerCase().includes(term)) ||
              (s.id && s.id.toString().includes(term))
            );
          }
          const totalCount = items.length;
          const startIndex = (page - 1) * pageSize;
          return {
            items: items.slice(startIndex, startIndex + pageSize),
            totalCount,
            pageNumber: page,
            pageSize,
            totalPages: Math.ceil(totalCount / pageSize) || 1
          };
        }
        // If backend returned paged object
        if (res && res.items) {
          return {
            items: res.items || [],
            totalCount: res.totalCount || 0,
            pageNumber: res.pageNumber || page,
            pageSize: res.pageSize || pageSize,
            totalPages: res.totalPages || Math.ceil((res.totalCount || 0) / pageSize) || 1
          };
        }
        return {
          items: [],
          totalCount: 0,
          pageNumber: page,
          pageSize,
          totalPages: 1
        };
      })
    );
  }

  // Get full list of suppliers without pagination (e.g. for dropdowns)
  getAllSuppliers(): Observable<Supplier[]> {
    return this.http.get<any>(`${this.baseUrl}/RMSSuppliers`).pipe(
      map(res => {
        if (Array.isArray(res)) return res;
        if (res && res.items) return res.items;
        return [];
      })
    );
  }

  getSupplierItems(supplierId: number): Observable<SupplierItem[]> {
    return this.http.get<SupplierItem[]>(`${this.baseUrl}/RMSSuppliers/${supplierId}/items`);
  }

  getSupplierStatement(
    supplierId: number,
    status?: string,
    section?: string,
    page: number = 1,
    pageSize: number = 10,
    search?: string
  ): Observable<SupplierStatement> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (status) params = params.set('status', status);
    if (section) params = params.set('section', section);
    if (search && search.trim()) params = params.set('search', search.trim());
    return this.http.get<SupplierStatement>(
      `${this.baseUrl}/RMSSuppliers/${supplierId}/statement`,
      { params }
    );
  }

  // Invoices
  getInvoices(): Observable<SupplierInvoice[]> {
    return this.http.get<SupplierInvoice[]>(`${this.baseUrl}/SupplierInvoices`);
  }

  // Server-side paged + filtered invoices (search, status, supplier)
  getInvoicesPaged(
    page: number = 1,
    pageSize: number = 10,
    search: string = '',
    status: string = '',
    supplierId?: number
  ): Observable<SupplierInvoicesPage> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search && search.trim()) params = params.set('search', search.trim());
    if (status && status !== 'ALL') params = params.set('status', status);
    if (supplierId) params = params.set('supplierId', supplierId.toString());
    return this.http.get<SupplierInvoicesPage>(`${this.baseUrl}/SupplierInvoices`, { params });
  }

  getInvoiceById(id: number): Observable<SupplierInvoice> {
    return this.http.get<SupplierInvoice>(`${this.baseUrl}/SupplierInvoices/${id}`);
  }

  createInvoice(request: CreateInvoiceRequest): Observable<SupplierInvoice> {
    const isCash = Number(request.paymentType) === 1;

    // Ensure payload conforms strictly to backend CreateSupplierInvoiceRequest
    // NOTE: CreditDays has [Range(1, int.MaxValue)] on backend; it MUST be undefined (null) for Cash, never 0.
    const payload = {
      rmsSupplierID: Number(request.rmsSupplierID || request.supplierId),
      invoiceNumber: request.invoiceNumber?.trim(),
      invoiceDate: request.invoiceDate ? request.invoiceDate : undefined,
      notes: request.notes?.trim() || undefined,
      paymentType: Number(request.paymentType),
      discountType: Number(request.discountType || 0),
      discountValue: request.discountType ? Number(request.discountValue || 0) : undefined,
      creditDays: (!isCash && request.creditDays && Number(request.creditDays) > 0) ? Number(request.creditDays) : undefined,
      initialPayment: isCash ? undefined : (request.initialPayment && Number(request.initialPayment) > 0 ? Number(request.initialPayment) : undefined),
      initialPaymentMethod: request.initialPaymentMethod?.trim() || (isCash ? 'Cash' : undefined),
      dueDate: (!isCash && request.dueDate && request.dueDate.trim()) ? request.dueDate : undefined,
      items: (request.items || []).map(item => ({
        rmsItemID: Number(item.rmsItemID || item.itemId),
        quantity: Number(item.quantity),
        unitPrice: Number(item.unitPrice)
      }))
    };
    return this.http.post<SupplierInvoice>(`${this.baseUrl}/SupplierInvoices`, payload);
  }

  addPayment(invoiceId: number, request: AddPaymentRequest): Observable<SupplierInvoice> {
    return this.http.post<SupplierInvoice>(`${this.baseUrl}/SupplierInvoices/${invoiceId}/payments`, request);
  }

  // Worksheets
  getWorksheets(): Observable<Worksheet[]> {
    return this.http.get<Worksheet[]>(`${this.baseUrl}/RMSWorksheets`);
  }

  getWorksheetById(id: number): Observable<Worksheet> {
    return this.http.get<Worksheet>(`${this.baseUrl}/RMSWorksheets/${id}`);
  }

  openWorksheet(dto: OpenWorksheetDto): Observable<{ worksheetId: number; status: number; message: string }> {
    return this.http.post<{ worksheetId: number; status: number; message: string }>(`${this.baseUrl}/RMSWorksheets`, dto);
  }

  openWorksheetWithPurchases(request: OpenWorksheetWithPurchasesRequest): Observable<{ worksheetId: number; status: number; message: string }> {
    return this.http.post<{ worksheetId: number; status: number; message: string }>(`${this.baseUrl}/RMSWorksheets/with-purchases`, request);
  }

  addPurchases(worksheetId: number, items: PurchaseLineDto[]): Observable<{ worksheetId: number; message: string }> {
    return this.http.post<{ worksheetId: number; message: string }>(`${this.baseUrl}/RMSWorksheets/${worksheetId}/purchases`, items);
  }

  transferWorksheet(request: { title?: string; notes?: string; storeId: number; items: PurchaseLineDto[] }): Observable<{ worksheetId: number; status: number; message: string }> {
    return this.http.post<{ worksheetId: number; status: number; message: string }>(`${this.baseUrl}/RMSWorksheets/transfer`, request);
  }

  approveWorksheet(worksheetId: number, destinationStoreId?: number | null): Observable<{ worksheetId: number; status: number; message: string }> {
    return this.http.post<{ worksheetId: number; status: number; message: string }>(`${this.baseUrl}/RMSWorksheets/${worksheetId}/approve`, { destinationStoreId: destinationStoreId ?? null });
  }
}
