import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MaintenanceService } from '../../../services/maintenance.service';
import {
  MaintenanceTicket,
  MaintenanceStatus,
  MaintenancePaymentMethod,
  AddTicketOperationRequest,
  CompleteRepairRequest,
  DeliverTicketRequest,
  AddTicketPaymentRequest
} from '../../../models/models';
import { PrintService } from '../../../shared/print/print.service';
import { ThermalReceiptService } from '../../../shared/print/thermal-receipt.service';
import { PrintSheetComponent } from '../../../shared/print/print-sheet.component';
import { TabbedSelectComponent, TabbedSelectItem } from '../../../components/tabbed-select/tabbed-select.component';

@Component({
  selector: 'app-maintenance-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TabbedSelectComponent, PrintSheetComponent],
  templateUrl: './maintenance-detail.component.html'
})
export class MaintenanceDetailComponent implements OnInit {
  ticketId!: number;
  ticket: MaintenanceTicket | null = null;
  loading: boolean = true;
  errorMessage: string = '';
  successMessage: string = '';

  // Quick standard operations with tabs & pagination
  standardOperations: TabbedSelectItem[] = [
    { id: 1, title: 'تغيير شاشة كاملة أصلية', category: 'شاشات', icon: 'smartphone' },
    { id: 2, title: 'تغيير باغة شاشة (Glass Only)', category: 'شاشات', icon: 'smartphone' },
    { id: 3, title: 'تغيير بطارية أصلية مع ضمان', category: 'بطاريات', icon: 'battery_charging_full' },
    { id: 4, title: 'صيانة سوكيت ومسار الشحن', category: 'شحن وباور', icon: 'power' },
    { id: 5, title: 'تغيير آيسي الشحن U2 / Tristar', category: 'شحن وباور', icon: 'memory' },
    { id: 6, title: 'تغيير فلاتة سماعة ومايك داخلي', category: 'صوتيات', icon: 'mic' },
    { id: 7, title: 'تغيير جرس خارجي / سبيكر', category: 'صوتيات', icon: 'volume_up' },
    { id: 8, title: 'تغيير كاميرا خلفية أصلية', category: 'كاميرات', icon: 'photo_camera' },
    { id: 9, title: 'تغيير كاميرا أمامية / سنسور', category: 'كاميرات', icon: 'camera_front' },
    { id: 10, title: 'سوفت وير وفك تشفير وفورمات', category: 'سوفت وير', icon: 'terminal' },
    { id: 11, title: 'تنظيف جاف ومعالجة دخول ماء وسوائل', category: 'صيانة عامة', icon: 'water_drop' },
    { id: 12, title: 'تغيير شاسيه وظهر كامل (Housing)', category: 'صيانة عامة', icon: 'aspect_ratio' }
  ];
  selectedOperationId: number | null = null;

  // Stage 2: Add Operation model («عملت كذا وسعرها كذا»)
  newOperation: AddTicketOperationRequest = {
    operationName: '',
    cost: 0,
    partCost: 0,
    notes: ''
  };
  addingOperation: boolean = false;

  // Complete repair model
  repairNotes: string = '';
  completingRepair: boolean = false;
  showCompleteRepairModal: boolean = false;

  // Stage 3: Delivery & Payment model
  deliveryForm: DeliverTicketRequest = {
    paymentAmount: 0,
    paymentMethod: MaintenancePaymentMethod.Cash,
    deliveryNotes: ''
  };
  delivering: boolean = false;
  showDeliveryModal: boolean = false;

  // Standalone Add Payment modal
  paymentForm: AddTicketPaymentRequest = {
    amount: 0,
    paymentMethod: MaintenancePaymentMethod.Cash,
    notes: ''
  };
  addingPayment: boolean = false;
  showPaymentModal: boolean = false;

  // Enums for template
  MaintenanceStatus = MaintenanceStatus;
  MaintenancePaymentMethod = MaintenancePaymentMethod;

  constructor(
    private route: ActivatedRoute,
    private maintenanceService: MaintenanceService,
    private printer: PrintService,
    private thermal: ThermalReceiptService
  ) { }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.ticketId = Number(idParam);
      this.fetchTicket();
    }
  }

  fetchTicket(): void {
    this.loading = true;
    this.errorMessage = '';
    this.maintenanceService.getTicketById(this.ticketId).subscribe({
      next: (data) => {
        this.ticket = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching ticket', err);
        this.errorMessage = 'تعذر تحميل بيانات التذكرة.';
        this.loading = false;
      }
    });
  }

  onSelectStandardOperation(item: TabbedSelectItem): void {
    this.selectedOperationId = Number(item.id);
    this.newOperation.operationName = item.title;
    this.newOperation.cost = item.price || 0;
  }

  onClearStandardOperation(): void {
    this.selectedOperationId = null;
    this.newOperation.operationName = '';
    this.newOperation.cost = 0;
    this.newOperation.partCost = 0;
  }

  // Stage 2: إضافة عملية إصلاح وتكلفتها
  onAddOperation(): void {
    if (!this.newOperation.operationName.trim()) {
      this.showMessage('يرجى كتابة اسم أو وصف العملية المنفذة أو اختيارها من القائمة.', true);
      return;
    }

    if ((this.newOperation.partCost ?? 0) < 0) {
      this.errorMessage = 'تكلفة القطعة لا يمكن أن تكون سالبة.';
      return;
    }

    if (this.newOperation.cost < 0) {
      this.showMessage('تكلفة العملية لا يمكن أن تكون سالبة.', true);
      return;
    }

    this.addingOperation = true;
    this.maintenanceService.addOperation(this.ticketId, this.newOperation).subscribe({
      next: (updatedTicket) => {
        this.ticket = updatedTicket;
        this.addingOperation = false;
        this.newOperation = { operationName: '', cost: 0, partCost: 0, notes: '' };
        this.selectedOperationId = null;
        this.showMessage('تم تسجيل عملية الإصلاح بنجاح.', false);
      },
      error: (err) => {
        this.addingOperation = false;
        this.showMessage(err.error?.message || 'حدث خطأ أثناء إضافة العملية.', true);
      }
    });
  }

  // حذف عملية إصلاح
  onDeleteOperation(operationId: number): void {
    if (!confirm('هل أنت متأكد من حذف هذه العملية؟')) return;

    this.maintenanceService.deleteOperation(this.ticketId, operationId).subscribe({
      next: (updatedTicket) => {
        this.ticket = updatedTicket;
        this.showMessage('تم حذف العملية بنجاح.', false);
      },
      error: (err) => {
        this.showMessage(err.error?.message || 'تعذر حذف العملية.', true);
      }
    });
  }

  // Stage 2: تأكيد انتهاء الإصلاح
  openCompleteRepairModal(): void {
    this.repairNotes = this.ticket?.repairNotes || '';
    this.showCompleteRepairModal = true;
  }

  onCompleteRepair(): void {
    this.completingRepair = true;
    const req: CompleteRepairRequest = {
      repairNotes: this.repairNotes
    };
    this.maintenanceService.completeRepair(this.ticketId, req).subscribe({
      next: (updatedTicket) => {
        this.ticket = updatedTicket;
        this.completingRepair = false;
        this.showCompleteRepairModal = false;
        this.showMessage('تم إتمام الصيانة بنجاح والجهاز جاهز للتسليم الآن!', false);
      },
      error: (err) => {
        this.completingRepair = false;
        this.showMessage(err.error?.message || 'حدث خطأ أثناء حفظ حالة الإصلاح.', true);
      }
    });
  }

  // Stage 3: تسليم الجهاز والدفع
  openDeliveryModal(): void {
    if (!this.ticket) return;
    this.deliveryForm = {
      paymentAmount: this.ticket.remainingAmount > 0 ? this.ticket.remainingAmount : 0,
      paymentMethod: MaintenancePaymentMethod.Cash,
      deliveryNotes: '',
      warrantyNote: ''
    };
    this.showDeliveryModal = true;
  }

  onDeliver(): void {
    this.delivering = true;
    this.maintenanceService.deliverTicket(this.ticketId, this.deliveryForm).subscribe({
      next: (updatedTicket) => {
        this.ticket = updatedTicket;
        this.delivering = false;
        this.showDeliveryModal = false;
        this.showMessage('تم تسليم الجهاز للعميل وتحصيل التكلفة بنجاح!', false);
        // الطباعة فوراً بعد التسليم والتحصيل
        this.thermal.printMaintenance(updatedTicket);
      },
      error: (err) => {
        this.delivering = false;
        this.showMessage(err.error?.message || 'حدث خطأ أثناء تسجيل تسليم الجهاز.', true);
      }
    });
  }

  // إضافة دفعة مستقلة
  openPaymentModal(): void {
    if (!this.ticket) return;
    this.paymentForm = {
      amount: this.ticket.remainingAmount > 0 ? this.ticket.remainingAmount : 0,
      paymentMethod: MaintenancePaymentMethod.Cash,
      notes: ''
    };
    this.showPaymentModal = true;
  }

  onAddPayment(): void {
    if (this.paymentForm.amount <= 0) {
      this.showMessage('المبلغ يجب أن يكون أكبر من صفر.', true);
      return;
    }

    this.addingPayment = true;
    this.maintenanceService.addPayment(this.ticketId, this.paymentForm).subscribe({
      next: (updatedTicket) => {
        this.ticket = updatedTicket;
        this.addingPayment = false;
        this.showPaymentModal = false;
        this.showMessage('تم تسجيل الدفعة بنجاح.', false);
      },
      error: (err) => {
        this.addingPayment = false;
        this.showMessage(err.error?.message || 'حدث خطأ أثناء إضافة الدفعة.', true);
      }
    });
  }

  // طباعة الإيصال
  /** فاتورة الحساب للعميل (Thermal Receipt 80mm): بنود الإصلاح والتكلفة وملاحظة الضمان */
  printThermal(): void {
    if (this.ticket) this.thermal.printMaintenance(this.ticket);
  }

  readonly warrantyChips = ['ضمان أسبوع', 'ضمان شهر', 'ضمان 3 شهور', 'ضمان 6 شهور', 'بدون ضمان'];

  printReceipt(): void {
    this.printer.print();
  }

  private showMessage(msg: string, isError: boolean): void {
    if (isError) {
      this.errorMessage = msg;
      this.successMessage = '';
    } else {
      this.successMessage = msg;
      this.errorMessage = '';
      setTimeout(() => (this.successMessage = ''), 5000);
    }
  }

  getStatusBadgeClass(status: MaintenanceStatus): string {
    switch (status) {
      case MaintenanceStatus.Received:
        return 'bg-blue-50 text-blue-700 border-blue-200';
      case MaintenanceStatus.UnderRepair:
        return 'bg-amber-50 text-amber-700 border-amber-200';
      case MaintenanceStatus.Completed:
        return 'bg-purple-50 text-purple-700 border-purple-200';
      case MaintenanceStatus.Delivered:
        return 'bg-emerald-50 text-emerald-700 border-emerald-200';
      case MaintenanceStatus.Cancelled:
        return 'bg-rose-50 text-rose-700 border-rose-200';
      default:
        return 'bg-gray-50 text-gray-700 border-gray-200';
    }
  }

  getStatusArabicName(status: MaintenanceStatus): string {
    switch (status) {
      case MaintenanceStatus.Received:
        return '1. تم الاستلام';
      case MaintenanceStatus.UnderRepair:
        return '2. قيد الفحص والإصلاح';
      case MaintenanceStatus.Completed:
        return '3. تم الإصلاح (جاهز للتسليم)';
      case MaintenanceStatus.Delivered:
        return '4. تم التسليم والدفع';
      case MaintenanceStatus.Cancelled:
        return 'ملغي';
      default:
        return 'غير محدد';
    }
  }

  getPaymentMethodName(method: MaintenancePaymentMethod): string {
    switch (method) {
      case MaintenancePaymentMethod.Cash:
        return 'نقدي (Cash)';
      case MaintenancePaymentMethod.CreditCard:
        return 'بطاقة بنكية (Credit Card)';
      case MaintenancePaymentMethod.ElectronicWallet:
        return 'محفظة إلكترونية (Vodafone / InstaPay)';
      default:
        return 'أخرى';
    }
  }
}
