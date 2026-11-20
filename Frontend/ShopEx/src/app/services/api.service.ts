import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  Supplier,
  SupplierItem,
  SupplierInvoice,
  CreateInvoiceRequest,
  AddPaymentRequest,
  Worksheet,
  OpenWorksheetDto,
  OpenWorksheetWithPurchasesRequest,
  PurchaseLineDto,
  PagedResponse,
  SupplierStatement
} from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = 'https://localhost:7122/api';

  constructor(private http: HttpClient) { }

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

  getSupplierStatement(supplierId: number): Observable<SupplierStatement> {
    return this.http.get<SupplierStatement>(`${this.baseUrl}/RMSSuppliers/${supplierId}/statement`);
  }

  // Invoices
  getInvoices(): Observable<SupplierInvoice[]> {
    return this.http.get<SupplierInvoice[]>(`${this.baseUrl}/SupplierInvoices`);
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

  approveWorksheet(worksheetId: number): Observable<{ worksheetId: number; status: number; message: string }> {
    return this.http.post<{ worksheetId: number; status: number; message: string }>(`${this.baseUrl}/RMSWorksheets/${worksheetId}/approve`, {});
  }
}
