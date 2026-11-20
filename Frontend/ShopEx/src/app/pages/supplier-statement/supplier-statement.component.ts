import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import {
  SupplierStatement,
  SupplierItem,
  SupplierInvoiceStatement,
  CreateInvoiceRequest,
  CreateInvoiceItemRequest,
  AddPaymentRequest
} from '../../models/models';

@Component({
  selector: 'app-supplier-statement',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './supplier-statement.component.html'
})
export class SupplierStatementComponent implements OnInit {
  supplierId!: number;
  statement: SupplierStatement | null = null;
  supplierItems: SupplierItem[] = [];

  loading: boolean = true;
  loadingItems: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // Invoice creation form state
  showCreateInvoiceForm: boolean = false;
  creatingInvoice: boolean = false;
  invoiceError: string = '';

  newInvoiceNumber: string = '';
  newInvoiceDate: string = new Date().toISOString().substring(0, 10);
  newPaymentType: number = 2; // 1 = Cash, 2 = Credit
  newCreditDays: number = 30;
  newDueDate: string = '';
  newInitialPayment: number = 0;
  newInitialPaymentMethod: string = 'Cash';
  newNotes: string = '';

  invoiceItems: {
    rmsItemID: number;
    description: string;
    quantity: number;
    unitPrice: number;
  }[] = [
    { rmsItemID: 0, description: '', quantity: 1, unitPrice: 0 }
  ];

  // Quick Payment Modal
  showPaymentModal: boolean = false;
  selectedInvoice: SupplierInvoiceStatement | null = null;
  paymentAmount: number = 0;
  paymentMethod: string = 'Cash';
  processingPayment: boolean = false;

  // Expanded Invoice Details
  expandedInvoiceId: number | null = null;

  constructor(
    private api: ApiService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      if (idParam) {
        this.supplierId = parseInt(idParam, 10);
        this.loadAllData();
      }
    });
    this.calculateDueDate();
  }

  loadAllData(): void {
    this.fetchStatement();
    this.fetchSupplierProducts();
  }

  fetchStatement(): void {
    this.loading = true;
    this.errorMessage = '';
    this.api.getSupplierStatement(this.supplierId).subscribe({
      next: (data) => {
        this.statement = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching statement', err);
        this.errorMessage = 'فشل في تحميل كشف حساب المورد. يرجى المحاولة مرة أخرى.';
        this.loading = false;
      }
    });
  }

  fetchSupplierProducts(): void {
    this.loadingItems = true;
    this.api.getSupplierItems(this.supplierId).subscribe({
      next: (items) => {
        this.supplierItems = items || [];
        this.loadingItems = false;
      },
      error: (err) => {
        console.error('Error fetching supplier items', err);
        this.loadingItems = false;
      }
    });
  }

  toggleCreateInvoice(): void {
    this.showCreateInvoiceForm = !this.showCreateInvoiceForm;
    if (this.showCreateInvoiceForm && !this.newInvoiceNumber) {
      this.generateInvoiceNumber();
    }
  }

  generateInvoiceNumber(): void {
    const d = new Date();
    const dateStr = d.toISOString().slice(0, 10).replace(/-/g, '');
    const rand = Math.floor(1000 + Math.random() * 9000);
    this.newInvoiceNumber = `INV-${this.supplierId}-${dateStr}-${rand}`;
  }

  onPaymentTypeChange(): void {
    if (this.newPaymentType === 1) {
      // Cash payment: payment is 100% of the invoice total
      this.newCreditDays = 0;
      this.newDueDate = '';
      this.newInitialPayment = this.computedTotal;
    } else {
      // Credit payment
      this.newCreditDays = 30;
      this.newInitialPayment = 0;
      this.calculateDueDate();
    }
  }

  calculateDueDate(): void {
    if (this.newPaymentType === 2 && this.newCreditDays > 0) {
      const d = new Date(this.newInvoiceDate || new Date());
      d.setDate(d.getDate() + Number(this.newCreditDays));
      this.newDueDate = d.toISOString().substring(0, 10);
    } else {
      this.newDueDate = '';
    }
  }

  onProductSelect(index: number, itemId: number): void {
    const item = this.supplierItems.find(i => i.id === Number(itemId));
    if (item) {
      this.invoiceItems[index].rmsItemID = item.id;
      this.invoiceItems[index].description = item.description;
      this.invoiceItems[index].unitPrice = item.cost || item.price || 0;
      if (this.newPaymentType === 1) {
        this.newInitialPayment = this.computedTotal;
      }
    }
  }

  onLineItemChange(): void {
    if (this.newPaymentType === 1) {
      this.newInitialPayment = this.computedTotal;
    }
  }

  addItemRow(): void {
    this.invoiceItems.push({
      rmsItemID: 0,
      description: '',
      quantity: 1,
      unitPrice: 0
    });
    if (this.newPaymentType === 1) {
      this.newInitialPayment = this.computedTotal;
    }
  }

  removeItemRow(index: number): void {
    if (this.invoiceItems.length > 1) {
      this.invoiceItems.splice(index, 1);
      if (this.newPaymentType === 1) {
        this.newInitialPayment = this.computedTotal;
      }
    }
  }

  get computedTotal(): number {
    return this.invoiceItems.reduce((acc, cur) => acc + ((cur.quantity || 0) * (cur.unitPrice || 0)), 0);
  }

  submitNewInvoice(): void {
    this.invoiceError = '';

    if (!this.newInvoiceNumber.trim()) {
      this.invoiceError = 'يرجى إدخال رقم الفاتورة.';
      return;
    }

    if (this.invoiceItems.some(i => !i.rmsItemID || i.rmsItemID <= 0 || i.quantity <= 0)) {
      this.invoiceError = 'يرجى اختيار منتج صالح وتحديد كمية أكبر من صفر لكل بند.';
      return;
    }

    const isCash = Number(this.newPaymentType) === 1;
    const initialPaymentAmount = isCash ? this.computedTotal : (this.newInitialPayment > 0 ? Number(this.newInitialPayment) : undefined);

    const payload: CreateInvoiceRequest = {
      rmsSupplierID: this.supplierId,
      invoiceNumber: this.newInvoiceNumber.trim(),
      invoiceDate: this.newInvoiceDate,
      paymentType: Number(this.newPaymentType),
      creditDays: isCash ? undefined : (this.newCreditDays > 0 ? Number(this.newCreditDays) : undefined),
      dueDate: (!isCash && this.newDueDate) ? this.newDueDate : undefined,
      notes: this.newNotes?.trim() || undefined,
      initialPayment: initialPaymentAmount,
      initialPaymentMethod: isCash ? (this.newInitialPaymentMethod || 'Cash') : (initialPaymentAmount && initialPaymentAmount > 0 ? this.newInitialPaymentMethod : undefined),
      items: this.invoiceItems.map(i => ({
        rmsItemID: Number(i.rmsItemID),
        quantity: Number(i.quantity),
        unitPrice: Number(i.unitPrice),
        description: i.description
      }))
    };

    this.creatingInvoice = true;
    this.api.createInvoice(payload).subscribe({
      next: (res) => {
        this.creatingInvoice = false;
        this.showCreateInvoiceForm = false;
        this.resetInvoiceForm();
        // Immediately navigate to the invoice details page and open print dialog
        this.router.navigate(['/invoices', res.id], { queryParams: { print: 'true' } });
      },
      error: (err) => {
        this.creatingInvoice = false;
        console.error('Error creating invoice', err);
        this.invoiceError = err.error?.message || err.message || 'حدث خطأ أثناء إنشاء الفاتورة.';
      }
    });
  }

  resetInvoiceForm(): void {
    this.newInvoiceNumber = '';
    this.newInvoiceDate = new Date().toISOString().substring(0, 10);
    this.newPaymentType = 2;
    this.newCreditDays = 30;
    this.newInitialPayment = 0;
    this.newNotes = '';
    this.invoiceItems = [
      { rmsItemID: 0, description: '', quantity: 1, unitPrice: 0 }
    ];
    this.calculateDueDate();
  }

  viewInvoice(id: number): void {
    this.router.navigate(['/invoices', id]);
  }

  toggleExpandInvoice(id: number, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.expandedInvoiceId = this.expandedInvoiceId === id ? null : id;
  }

  openPaymentModal(inv: SupplierInvoiceStatement): void {
    this.selectedInvoice = inv;
    this.paymentAmount = inv.remainingAmount;
    this.paymentMethod = 'Cash';
    this.showPaymentModal = true;
  }

  closePaymentModal(): void {
    this.showPaymentModal = false;
    this.selectedInvoice = null;
  }

  submitPayment(): void {
    if (!this.selectedInvoice || this.paymentAmount <= 0) return;

    this.processingPayment = true;
    const req: AddPaymentRequest = {
      amount: Number(this.paymentAmount),
      paymentMethod: this.paymentMethod,
      paymentDate: new Date().toISOString()
    };

    this.api.addPayment(this.selectedInvoice.id, req).subscribe({
      next: () => {
        this.processingPayment = false;
        this.closePaymentModal();
        this.successMessage = `تم تسجيل دفعة بقيمة ${req.amount} بنجاح!`;
        this.fetchStatement();
        setTimeout(() => this.successMessage = '', 5000);
      },
      error: (err) => {
        this.processingPayment = false;
        console.error('Error recording payment', err);
        alert(err.error?.message || 'فشل في تسجيل الدفعة.');
      }
    });
  }
}

