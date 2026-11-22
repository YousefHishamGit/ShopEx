import { API_URL } from '../core/api-config';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  MaintenanceTicket,
  MaintenanceTicketSummary,
  MaintenanceStatus,
  CreateMaintenanceTicketRequest,
  AddTicketOperationRequest,
  CompleteRepairRequest,
  DeliverTicketRequest,
  AddTicketPaymentRequest
} from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class MaintenanceService {
  private readonly baseUrl = API_URL + '/MaintenanceTickets';

  constructor(private http: HttpClient) {}

  // استرجاع كل التذاكر مع فلترة اختيارية
  getTickets(status?: MaintenanceStatus, customerPhone?: string): Observable<MaintenanceTicketSummary[]> {
    let params = new HttpParams();
    if (status !== undefined && status !== null) {
      params = params.set('status', status.toString());
    }
    if (customerPhone && customerPhone.trim()) {
      params = params.set('customerPhone', customerPhone.trim());
    }
    return this.http.get<MaintenanceTicketSummary[]>(this.baseUrl, { params });
  }

  // استرجاع تذكرة بالتفصيل بالمعرف
  getTicketById(id: number): Observable<MaintenanceTicket> {
    return this.http.get<MaintenanceTicket>(`${this.baseUrl}/${id}`);
  }

  // استرجاع تذكرة برقم الإيصال
  getTicketByNumber(ticketNumber: string): Observable<MaintenanceTicket> {
    return this.http.get<MaintenanceTicket>(`${this.baseUrl}/by-number/${encodeURIComponent(ticketNumber.trim())}`);
  }

  // المرحلة الأولى: إنشاء واستلام تذكرة صيانة
  createTicket(request: CreateMaintenanceTicketRequest): Observable<MaintenanceTicket> {
    const payload = {
      receivedBy: request.receivedBy?.trim() || undefined,
      customerName: request.customerName.trim(),
      customerPhone: request.customerPhone.trim(),
      deviceName: request.deviceName.trim(),
      brand: request.brand?.trim() || undefined,
      model: request.model?.trim() || undefined,
      color: request.color?.trim() || undefined,
      serialNumber: request.serialNumber?.trim() || undefined,
      issueDescription: request.issueDescription?.trim() || undefined,
      accessories: request.accessories?.trim() || undefined,
      notes: request.notes?.trim() || undefined,
      expectedCost: Number(request.expectedCost || 0),
      deposit: request.deposit ? Number(request.deposit) : undefined,
      depositPaymentMethod: request.depositPaymentMethod ? Number(request.depositPaymentMethod) : undefined
    };
    return this.http.post<MaintenanceTicket>(this.baseUrl, payload);
  }

  // المرحلة الثانية: إضافة عملية أو قطعة تم إصلاحها («عملت كذا وسعرها كذا»)
  addOperation(ticketId: number, request: AddTicketOperationRequest): Observable<MaintenanceTicket> {
    const payload = {
      operationName: request.operationName.trim(),
      cost: Number(request.cost || 0),
      notes: request.notes?.trim() || undefined
    };
    return this.http.post<MaintenanceTicket>(`${this.baseUrl}/${ticketId}/operations`, payload);
  }

  // حذف عملية إصلاح
  deleteOperation(ticketId: number, operationId: number): Observable<MaintenanceTicket> {
    return this.http.delete<MaintenanceTicket>(`${this.baseUrl}/${ticketId}/operations/${operationId}`);
  }

  // المرحلة الثانية: تأكيد إتمام الصيانة وجاهزية الجهاز للتسليم
  completeRepair(ticketId: number, request?: CompleteRepairRequest): Observable<MaintenanceTicket> {
    const payload = {
      repairNotes: request?.repairNotes?.trim() || undefined,
      operations: request?.operations?.map(op => ({
        operationName: op.operationName.trim(),
        cost: Number(op.cost || 0),
        notes: op.notes?.trim() || undefined
      }))
    };
    return this.http.post<MaintenanceTicket>(`${this.baseUrl}/${ticketId}/complete-repair`, payload);
  }

  // المرحلة الثالثة: إضافة دفعة نقدية
  addPayment(ticketId: number, request: AddTicketPaymentRequest): Observable<MaintenanceTicket> {
    const payload = {
      amount: Number(request.amount),
      paymentMethod: Number(request.paymentMethod),
      notes: request.notes?.trim() || undefined,
      paidAt: request.paidAt || undefined
    };
    return this.http.post<MaintenanceTicket>(`${this.baseUrl}/${ticketId}/payments`, payload);
  }

  // المرحلة الثالثة: تسليم الجهاز للعميل وسداد التكلفة المتبقية
  deliverTicket(ticketId: number, request?: DeliverTicketRequest): Observable<MaintenanceTicket> {
    const payload = {
      paymentAmount: request?.paymentAmount !== undefined ? Number(request.paymentAmount) : undefined,
      paymentMethod: request?.paymentMethod ? Number(request.paymentMethod) : 1,
      deliveryNotes: request?.deliveryNotes?.trim() || undefined
    };
    return this.http.post<MaintenanceTicket>(`${this.baseUrl}/${ticketId}/deliver`, payload);
  }

  // تحديث الحالة يدوياً
  updateStatus(ticketId: number, status: MaintenanceStatus): Observable<MaintenanceTicket> {
    return this.http.patch<MaintenanceTicket>(`${this.baseUrl}/${ticketId}/status`, status);
  }
}
