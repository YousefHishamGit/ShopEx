import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { StockService } from '../../services/stock.service';
import { SupplierInvoice } from '../../models/models';
import { SupplierReturnService } from '../../services/supplier-return.service';
import { ToastService } from '../../services/toast.service';
import {
  SupplierStatement,
  SupplierItem,
  SupplierInvoiceStatement,
  CreateInvoiceRequest,
  AddPaymentRequest,
  Warehouse,
  CreateSupplierReturnRequest,
  SupplierReturnSettlementType,
  SupplierReturn,
  SupplierReturnItem,
  SupplierReturnStatement,
  AddSupplierRefundRequest,
  SupplierRefund
} from '../../models/models';
import { TabbedSelectComponent, TabbedSelectItem } from '../../components/tabbed-select/tabbed-select.component';

@Component({
  selector: 'app-supplier-statement',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TabbedSelectComponent],
  templateUrl: './supplier-statement.component.html'
})
export class SupplierStatementComponent implements OnInit {
  supplierId!: number;
  statement: SupplierStatement | null = null;
  supplierItems: SupplierItem[] = [];

  loading: boolean = false;        // first page load only
  loadingRows: boolean = false;    // table refresh (search / filter / page) - keeps the page mounted
  private invoiceSeq = 0;
  private returnSeq = 0;
  private summaryLoaded = false;
  loadingItems: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // Main Section Toggle: 'invoices' or 'returns'
  activeSection: 'invoices' | 'returns' = 'invoices';

  // 3 Strict Invoices Filters: No "All"
  invoiceFilter: 'unpaid' | 'partial' | 'paid' = 'paid';
  invoiceSearch: string = '';

  // Pagination state (mirrors what server returns)
  invoicePage: number = 1;
  invoicePageSize: number = 10;
  invoiceTotalPages: number = 1;
  invoiceTotalCount: number = 0;

  returnPage: number = 1;
  returnPageSize: number = 10;
  returnTotalPages: number = 1;
  returnTotalCount: number = 0;

  // Counts from server
  unpaidCount: number = 0;
  partialCount: number = 0;
  paidCount: number = 0;
  totalInvoicesCount: number = 0;
  returnsCount: number = 0;

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
  newInitialPaymentMethod: string = 'نقدي';
  newNotes: string = '';
  newDiscountType: number = 0;   // 0 none, 1 percent, 2 amount
  newDiscountValue: number = 0;

  invoiceItems: {
    rmsItemID: number;
    description: string;
    quantity: number;
    unitPrice: number;
  }[] = [
      { rmsItemID: 0, description: '', quantity: 1, unitPrice: 0 }
    ];

  // Return creation form state (مدمج زي إنشاء الفاتورة)
  showCreateReturnForm: boolean = false;
  creatingReturn: boolean = false;
  returnError: string = '';

  warehouses: Warehouse[] = [];
  returnWarehouseId: number | null = null;
  returnDate: string = new Date().toISOString().substring(0, 10);
  returnSettlementType: number = SupplierReturnSettlementType.CreditToAccount;
  returnSupplierInvoiceId: number | null = null;
  returnInvoiceOptions: SupplierInvoice[] = [];
  loadingInvoiceOptions: boolean = false;
  returnNotes: string = '';

  // Stock tracking for selected warehouse
  warehouseStockMap: Map<number, number> = new Map<number, number>();
  loadingWarehouseStock: boolean = false;

  returnItems: {
    rmsItemID: number;
    description: string;
    quantity: number;
    unitPrice: number;
    reason: string;
    maxQuantity?: number;
  }[] = [
      { rmsItemID: 0, description: '', quantity: 1, unitPrice: 0, reason: '', maxQuantity: 0 }
    ];

  // Return Details Expansion & Actions in statement table
  expandedReturnId: number | null = null;
  expandedReturnData: SupplierReturn | null = null;
  loadingReturnDetails: boolean = false;
  confirmingReturnId: number | null = null;
  cancellingReturnId: number | null = null;

  // Quick Refund Modal (استرداد نقدية من المورد)
  showRefundModal: boolean = false;
  selectedReturnForRefund: SupplierReturnStatement | null = null;
  refundAmount: number = 0;
  refundPaymentMethod: string = 'Cash';
  refundDate: string = new Date().toISOString().substring(0, 10);
  refundNotes: string = '';
  processingRefund: boolean = false;

  // Quick Payment Modal
  showPaymentModal: boolean = false;
  selectedInvoice: SupplierInvoiceStatement | null = null;
  paymentAmount: number = 0;
  paymentMethod: string = 'Cash';
  processingPayment: boolean = false;

  // Expanded Invoice Details
  expandedInvoiceId: number | null = null;

  private searchDebounce: any;

  constructor(
    private api: ApiService,
    private stockService: StockService,
    private supplierReturnService: SupplierReturnService,
    private toast: ToastService,
    private route: ActivatedRoute,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      if (idParam) {
        this.supplierId = parseInt(idParam, 10);
        this.statement = null;
        this.summaryLoaded = false;
        this.returnInvoiceOptions = [];
        this.expandedInvoiceId = null;
        this.expandedReturnId = null;
        // Load supplier info + counts only (no section data yet)
        this.fetchSummary();
        this.fetchSupplierProducts();
        this.loadWarehouses();
      }
    });

    this.route.queryParams.subscribe(params => {
      if (params['return'] === 'true') {
        this.showCreateReturnForm = true;
        this.showCreateInvoiceForm = false;
        this.activeSection = 'returns';
        this.loadWarehouses();
      }
    });

    this.calculateDueDate();
  }

  // ─────────────────────────────────────────────
  // FETCH: summary (counts + header info, no rows)
  // Called once on page load to get totals and counts
  // ─────────────────────────────────────────────
  fetchSummary(): void {
    if (!this.summaryLoaded) this.loading = true;
    this.errorMessage = '';
    // 'summary' section => header + totals + counts only (no rows are queried)
    this.api.getSupplierStatement(this.supplierId, undefined, 'summary', 1, 1).subscribe({
      next: (data) => {
        const prev = this.statement;
        if (prev) {
          data.invoices = prev.invoices;
          data.returns = prev.returns;
        }
        this.statement = data;
        this.unpaidCount = data.unpaidCount ?? 0;
        this.partialCount = data.partialCount ?? 0;
        this.paidCount = data.paidCount ?? 0;
        this.totalInvoicesCount = data.totalInvoicesCount ?? 0;
        this.returnsCount = data.returnsCount ?? 0;
        this.loading = false;

        if (!this.summaryLoaded) {
          this.summaryLoaded = true;
          // Auto-pick the most relevant invoice tab and load it right away
          this.invoiceFilter = this.unpaidCount > 0 ? 'unpaid' : (this.partialCount > 0 ? 'partial' : 'paid');
          if (this.activeSection === 'returns') {
            this.fetchReturns(true);
          } else {
            this.fetchInvoices(true);
          }
        }
      },
      error: (err) => {
        console.error('Error fetching statement summary', err);
        this.errorMessage = 'فشل في تحميل بيانات المورد. يرجى المحاولة مرة أخرى.';
        this.loading = false;
      }
    });
  }

  fetchInvoices(resetPage: boolean = false): void {
    if (resetPage) this.invoicePage = 1;
    this.loadingRows = true;
    this.errorMessage = '';
    const seq = ++this.invoiceSeq;
    this.api.getSupplierStatement(
      this.supplierId,
      this.invoiceFilter,
      'invoices',
      this.invoicePage,
      this.invoicePageSize,
      this.invoiceSearch || undefined
    ).subscribe({
      next: (data) => {
        if (seq !== this.invoiceSeq) return; // stale response
        if (this.statement) {
          data.returns = this.statement.returns;
        }
        this.statement = data;
        this.unpaidCount = data.unpaidCount ?? this.unpaidCount;
        this.partialCount = data.partialCount ?? this.partialCount;
        this.paidCount = data.paidCount ?? this.paidCount;
        this.totalInvoicesCount = data.totalInvoicesCount ?? this.totalInvoicesCount;
        this.returnsCount = data.returnsCount ?? this.returnsCount;
        this.invoicePage = data.invoicesPageNumber;
        this.invoicePageSize = data.invoicesPageSize;
        this.invoiceTotalPages = data.invoicesTotalPages;
        this.invoiceTotalCount = data.invoicesTotalCount;
        this.loadingRows = false;
      },
      error: (err) => {
        if (seq !== this.invoiceSeq) return;
        console.error('Error fetching invoices', err);
        this.errorMessage = 'تعذر تحميل الفواتير. حاول مرة أخرى.';
        this.loadingRows = false;
      }
    });
  }

  fetchReturns(resetPage: boolean = false): void {
    if (resetPage) this.returnPage = 1;
    this.loadingRows = true;
    this.errorMessage = '';
    const seq = ++this.returnSeq;
    this.api.getSupplierStatement(
      this.supplierId,
      undefined,
      'returns',
      this.returnPage,
      this.returnPageSize
    ).subscribe({
      next: (data) => {
        if (seq !== this.returnSeq) return;
        if (this.statement) {
          data.invoices = this.statement.invoices;
        }
        this.statement = data;
        this.unpaidCount = data.unpaidCount ?? this.unpaidCount;
        this.partialCount = data.partialCount ?? this.partialCount;
        this.paidCount = data.paidCount ?? this.paidCount;
        this.totalInvoicesCount = data.totalInvoicesCount ?? this.totalInvoicesCount;
        this.returnsCount = data.returnsCount ?? this.returnsCount;
        this.returnPage = data.returnsPageNumber;
        this.returnPageSize = data.returnsPageSize;
        this.returnTotalPages = data.returnsTotalPages;
        this.returnTotalCount = data.returnsTotalCount;
        this.loadingRows = false;
      },
      error: (err) => {
        if (seq !== this.returnSeq) return;
        console.error('Error fetching returns', err);
        this.errorMessage = 'تعذر تحميل المرتجعات. حاول مرة أخرى.';
        this.loadingRows = false;
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


  setSection(section: 'invoices' | 'returns'): void {
    this.activeSection = section;
    if (section === 'returns') {
      this.fetchReturns(true);
    } else {
      this.fetchInvoices(true);
    }
  }


  setInvoiceFilter(filter: 'unpaid' | 'partial' | 'paid'): void {
    this.invoiceFilter = filter;
    this.fetchInvoices(true);
  }


  onInvoiceSearchChange(): void {
    if (this.searchDebounce) clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => {
      this.fetchInvoices(true);
    }, 400);
  }


  setInvoicePage(page: number): void {
    if (page >= 1 && page <= this.invoiceTotalPages && page !== this.invoicePage) {
      this.invoicePage = page;
      this.fetchInvoices(false);
    }
  }

  get visibleInvoicePages(): number[] {
    return this.buildPageRange(this.invoicePage, this.invoiceTotalPages);
  }

  setReturnPage(page: number): void {
    if (page >= 1 && page <= this.returnTotalPages && page !== this.returnPage) {
      this.returnPage = page;
      this.fetchReturns(false);
    }
  }

  get visibleReturnPages(): number[] {
    return this.buildPageRange(this.returnPage, this.returnTotalPages);
  }

  private buildPageRange(current: number, total: number): number[] {
    const pages: number[] = [];
    let start = Math.max(1, current - 2);
    let end = Math.min(total, current + 2);
    if (current <= 3) end = Math.min(5, total);
    if (current >= total - 2) start = Math.max(1, total - 4);
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  }

  get selectableProducts(): TabbedSelectItem[] {
    return this.supplierItems.map(item => ({
      id: item.id,
      title: item.description,
      subtitle: item.itemLookupCode ? `باركود: ${item.itemLookupCode}` : `صنف #${item.id}`,
      price: item.cost || item.price || 0,
      cost: item.cost,
      quantity: item.quantity,
      icon: 'inventory_2',
      raw: item
    }));
  }

  onTabbedProductSelect(index: number, selected: TabbedSelectItem): void {
    this.invoiceItems[index].rmsItemID = Number(selected.id);
    this.invoiceItems[index].description = selected.title;
    this.invoiceItems[index].unitPrice = selected.price || selected.cost || 0;
    if (this.newPaymentType === 1) {
      this.newInitialPayment = this.computedTotal;
    }
  }

  onTabbedProductClear(index: number): void {
    this.invoiceItems[index].rmsItemID = 0;
    this.invoiceItems[index].description = '';
    this.invoiceItems[index].unitPrice = 0;
  }

  // Latest invoices of this supplier, for the 'original invoice' dropdown (lazy, one small request)
  loadInvoiceOptions(): void {
    if (this.returnInvoiceOptions.length > 0 || this.loadingInvoiceOptions) return;
    this.loadingInvoiceOptions = true;
    this.api.getInvoicesPaged(1, 100, '', '', this.supplierId).subscribe({
      next: (res) => {
        this.returnInvoiceOptions = res.items || [];
        this.loadingInvoiceOptions = false;
      },
      error: () => { this.loadingInvoiceOptions = false; }
    });
  }

  loadWarehouses(): void {
    if (this.warehouses.length > 0) {
      if (this.returnWarehouseId && this.warehouseStockMap.size === 0) {
        this.loadWarehouseStock();
      }
      return;
    }
    this.stockService.getWarehouses(true).subscribe({
      next: (data) => {
        this.warehouses = data || [];
        if (this.warehouses.length > 0 && !this.returnWarehouseId) {
          const defaultWh = this.warehouses.find(w => w.isDefault) || this.warehouses[0];
          this.returnWarehouseId = defaultWh.id;
          this.loadWarehouseStock();
        }
      },
      error: (err) => console.error('Error loading warehouses', err)
    });
  }

  loadWarehouseStock(): void {
    if (!this.returnWarehouseId) {
      this.warehouseStockMap.clear();
      return;
    }
    this.loadingWarehouseStock = true;
    this.stockService.getAllBalances(this.returnWarehouseId).subscribe({
      next: (balances) => {
        this.warehouseStockMap.clear();
        if (balances) {
          for (const b of balances) {
            if (b.quantityOnHand > 0) {
              this.warehouseStockMap.set(b.rmsItemID, b.quantityOnHand);
            }
          }
        }
        this.loadingWarehouseStock = false;
        this.syncReturnItemsWithStock();
      },
      error: (err) => {
        console.error('Error loading warehouse stock', err);
        this.loadingWarehouseStock = false;
      }
    });
  }

  onReturnWarehouseChange(): void {
    this.loadWarehouseStock();
  }

  syncReturnItemsWithStock(): void {
    for (const it of this.returnItems) {
      if (it.rmsItemID) {
        const stock = this.warehouseStockMap.get(it.rmsItemID) || 0;
        it.maxQuantity = stock;
        if (it.quantity > stock && stock > 0) {
          it.quantity = stock;
        }
      }
    }
  }

  get selectableReturnProducts(): TabbedSelectItem[] {
    if (!this.returnWarehouseId || !this.supplierItems || this.supplierItems.length === 0) {
      return [];
    }

    // Only products of this supplier that have positive stock in the selected warehouse
    const inStockItems = this.supplierItems.filter(item => {
      const stock = this.warehouseStockMap.get(item.id) || 0;
      return stock > 0;
    });

    return inStockItems.map(item => {
      const stock = this.warehouseStockMap.get(item.id) || 0;
      return {
        id: item.id,
        title: item.description,
        subtitle: `المتاح بالمخزن: ${stock}` + (item.itemLookupCode ? ` | باركود: ${item.itemLookupCode}` : ''),
        price: item.cost || item.price || 0,
        cost: item.cost,
        quantity: stock,
        icon: 'inventory_2',
        raw: item
      };
    });
  }

  toggleCreateInvoice(): void {
    this.showCreateInvoiceForm = !this.showCreateInvoiceForm;
    if (this.showCreateInvoiceForm) {
      this.showCreateReturnForm = false;
      if (!this.newInvoiceNumber) {
        this.generateInvoiceNumber();
      }
    }
  }

  toggleCreateReturn(): void {
    this.showCreateReturnForm = !this.showCreateReturnForm;
    if (this.showCreateReturnForm) {
      this.showCreateInvoiceForm = false;
      this.loadWarehouses();
      this.loadInvoiceOptions();
      if (!this.supplierItems || this.supplierItems.length === 0) {
        this.fetchSupplierProducts();
      }
      this.loadWarehouseStock();
    }
  }

  addReturnItemRow(): void {
    this.returnItems.push({
      rmsItemID: 0,
      description: '',
      quantity: 1,
      unitPrice: 0,
      reason: '',
      maxQuantity: 0
    });
  }

  removeReturnItemRow(index: number): void {
    if (this.returnItems.length > 1) {
      this.returnItems.splice(index, 1);
    }
  }

  onTabbedReturnProductSelect(index: number, selected: TabbedSelectItem): void {
    this.returnItems[index].rmsItemID = Number(selected.id);
    this.returnItems[index].description = selected.title;
    this.returnItems[index].unitPrice = selected.cost || selected.price || 0;
    const stock = selected.quantity || (this.warehouseStockMap.get(Number(selected.id)) || 0);
    this.returnItems[index].maxQuantity = stock;
    if (this.returnItems[index].quantity > stock) {
      this.returnItems[index].quantity = stock > 0 ? stock : 1;
    }
  }

  onTabbedReturnProductClear(index: number): void {
    this.returnItems[index].rmsItemID = 0;
    this.returnItems[index].description = '';
    this.returnItems[index].unitPrice = 0;
    this.returnItems[index].maxQuantity = 0;
  }

  get computedReturnTotal(): number {
    return this.returnItems.reduce((acc, cur) => acc + ((cur.quantity || 0) * (cur.unitPrice || 0)), 0);
  }

  resetReturnForm(): void {
    this.returnDate = new Date().toISOString().substring(0, 10);
    this.returnSettlementType = SupplierReturnSettlementType.CreditToAccount;
    this.returnSupplierInvoiceId = null;
    this.returnNotes = '';
    this.returnItems = [
      { rmsItemID: 0, description: '', quantity: 1, unitPrice: 0, reason: '', maxQuantity: 0 }
    ];
    if (this.warehouses.length > 0) {
      const defaultWh = this.warehouses.find(w => w.isDefault) || this.warehouses[0];
      this.returnWarehouseId = defaultWh.id;
      this.loadWarehouseStock();
    }
  }

  submitNewReturn(): void {
    this.returnError = '';

    if (!this.returnWarehouseId) {
      this.returnError = 'يرجى اختيار المخزن المراد الإرجاع منه.';
      return;
    }

    if (this.returnItems.some(i => !i.rmsItemID || i.rmsItemID <= 0 || !(Number(i.quantity) > 0))) {
      this.returnError = 'يرجى اختيار صنف صالح وتحديد كمية أكبر من صفر لكل بند.';
      return;
    }

    if (this.returnItems.some(i => Number(i.unitPrice) < 0 || isNaN(Number(i.unitPrice)))) {
      this.returnError = 'سعر الوحدة لا يمكن أن يكون سالباً.';
      return;
    }

    // Same item on several rows must not exceed the warehouse balance together
    const perItem = new Map<number, number>();
    for (const it of this.returnItems) {
      perItem.set(it.rmsItemID, (perItem.get(it.rmsItemID) || 0) + Number(it.quantity));
    }
    for (const [itemId, qty] of perItem) {
      const available = this.warehouseStockMap.get(itemId) ?? 0;
      if (qty > available) {
        const name = this.returnItems.find(r => r.rmsItemID === itemId)?.description || ('#' + itemId);
        this.returnError = `الكمية المطلوبة للصنف (${name}) = ${qty} أكبر من الرصيد المتاح بالمخزن = ${available}.`;
        return;
      }
    }

    const payload: CreateSupplierReturnRequest = {
      rmsSupplierID: this.supplierId,
      warehouseID: Number(this.returnWarehouseId),
      supplierInvoiceID: this.returnSupplierInvoiceId ? Number(this.returnSupplierInvoiceId) : undefined,
      returnDate: this.returnDate,
      settlementType: Number(this.returnSettlementType),
      notes: this.returnNotes?.trim() || undefined,
      confirmImmediately: true,
      items: this.returnItems.map(i => ({
        rmsItemID: Number(i.rmsItemID),
        quantity: Number(i.quantity),
        unitPrice: Number(i.unitPrice),
        reason: i.reason?.trim() || undefined
      }))
    };

    this.creatingReturn = true;
    this.supplierReturnService.createReturn(payload).subscribe({
      next: (res) => {
        this.creatingReturn = false;
        this.showCreateReturnForm = false;
        this.resetReturnForm();
        this.successMessage = `تم إنشاء وتأكيد المرتجع ${res.returnNumber || res.id}`;
        this.refreshReturnsView();
        setTimeout(() => this.successMessage = '', 6000);
      },
      error: (err) => {
        this.creatingReturn = false;
        console.error('Error creating return', err);
        this.returnError = err.error?.message || err.message || 'حدث خطأ أثناء حفظ المرتجع.';
      }
    });
  }

  // Single refresh path: totals/counts + the returns rows, sequenced so neither overwrites the other
  private refreshReturnsView(): void {
    this.activeSection = 'returns';
    this.fetchReturns(false);
    this.loadWarehouseStock();
  }


  openRefundModal(ret: SupplierReturnStatement): void {
    this.selectedReturnForRefund = ret;
    const remaining = (ret.totalAmount - ret.refundedAmount);
    this.refundAmount = remaining > 0 ? remaining : 0;
    this.refundPaymentMethod = 'Cash';
    this.refundDate = new Date().toISOString().substring(0, 10);
    this.refundNotes = '';
    this.showRefundModal = true;
  }

  closeRefundModal(): void {
    this.showRefundModal = false;
    this.selectedReturnForRefund = null;
  }

  submitRefund(): void {
    if (!this.selectedReturnForRefund || this.refundAmount <= 0) return;

    this.processingRefund = true;
    const req: AddSupplierRefundRequest = {
      amount: Number(this.refundAmount),
      paymentMethod: this.refundPaymentMethod,
      refundDate: this.refundDate,
      notes: this.refundNotes?.trim() || undefined
    };

    this.supplierReturnService.addRefund(this.selectedReturnForRefund.id, req).subscribe({
      next: (updatedReturn) => {
        this.processingRefund = false;
        this.closeRefundModal();
        this.successMessage = `تم تسجيل استرداد مبلغ ${req.amount} ج.م من المورد بنجاح!`;
        if (this.expandedReturnId === this.selectedReturnForRefund?.id) {
          this.expandedReturnData = updatedReturn;
        }
        this.fetchReturns(false);
        setTimeout(() => this.successMessage = '', 6000);
      },
      error: (err) => {
        this.processingRefund = false;
        console.error('Error adding refund', err);
        this.toast.error(err.error?.message || err.message || 'فشل في تسجيل الاسترداد.');
      }
    });
  }


  toggleExpandReturn(retId: number): void {
    if (this.expandedReturnId === retId) {
      this.expandedReturnId = null;
      this.expandedReturnData = null;
      return;
    }

    this.expandedReturnId = retId;
    this.expandedReturnData = null;
    this.loadingReturnDetails = true;

    this.supplierReturnService.getReturnById(retId).subscribe({
      next: (data) => {
        this.expandedReturnData = data;
        this.loadingReturnDetails = false;
      },
      error: (err) => {
        console.error('Error fetching return details', err);
        this.loadingReturnDetails = false;
      }
    });
  }

  confirmReturn(ret: any): void {
    if (!confirm(`هل أنت متأكد من تأكيد المرتجع رقم ${ret.returnNumber}؟ سيتم خصم الكميات من رصيد المخزن فورياً وتطبيق الأثر المالي.`)) {
      return;
    }

    this.confirmingReturnId = ret.id;
    this.supplierReturnService.confirmReturn(ret.id).subscribe({
      next: (updated) => {
        this.confirmingReturnId = null;
        this.successMessage = `تم تأكيد المرتجع رقم ${ret.returnNumber} بنجاح وخصم الأصناف من المخزن!`;
        if (this.expandedReturnId === ret.id) {
          this.expandedReturnData = updated;
        }
        this.fetchReturns(false);
        this.loadWarehouseStock();
        setTimeout(() => this.successMessage = '', 6000);
      },
      error: (err) => {
        this.confirmingReturnId = null;
        console.error('Error confirming return', err);
        this.toast.error(err.error?.message || 'فشل تأكيد المرتجع.');
      }
    });
  }

  cancelReturn(ret: any): void {
    if (!confirm(`هل أنت متأكد من إلغاء المرتجع رقم ${ret.returnNumber}؟`)) {
      return;
    }

    this.cancellingReturnId = ret.id;
    this.supplierReturnService.cancelReturn(ret.id).subscribe({
      next: (updated) => {
        this.cancellingReturnId = null;
        this.successMessage = `تم إلغاء المرتجع رقم ${ret.returnNumber} بنجاح.`;
        if (this.expandedReturnId === ret.id) {
          this.expandedReturnData = updated;
        }
        this.fetchReturns(false);
        setTimeout(() => this.successMessage = '', 6000);
      },
      error: (err) => {
        this.cancellingReturnId = null;
        console.error('Error cancelling return', err);
        this.toast.error(err.error?.message || 'فشل إلغاء المرتجع.');
      }
    });
  }

  getItemName(rmsItemId: number): string {
    const item = this.supplierItems.find(i => i.id === rmsItemId);
    return item ? item.description : `صنف #${rmsItemId}`;
  }

  generateInvoiceNumber(): void {
    const d = new Date();
    const dateStr = d.toISOString().slice(0, 10).replace(/-/g, '');
    const rand = Math.floor(1000 + Math.random() * 9000);
    this.newInvoiceNumber = `INV-${this.supplierId}-${dateStr}-${rand}`;
  }

  onPaymentTypeChange(): void {
    if (this.newPaymentType === 1) {
      this.newCreditDays = 0;
      this.newDueDate = '';
      this.newInitialPayment = this.computedTotal;
    } else {
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

  /** Sum of the item lines before discount. */
  get computedSubTotal(): number {
    return this.invoiceItems.reduce((acc, cur) => acc + ((cur.quantity || 0) * (cur.unitPrice || 0)), 0);
  }

  /** Discount resolved to an amount (never more than the subtotal). */
  get computedDiscount(): number {
    const sub = this.computedSubTotal;
    const v = Number(this.newDiscountValue) || 0;
    if (this.newDiscountType === 1) return Math.min(sub, Math.round(sub * Math.min(v, 100)) / 100);
    if (this.newDiscountType === 2) return Math.min(sub, v);
    return 0;
  }

  get newDiscountError(): string {
    const v = Number(this.newDiscountValue) || 0;
    if (this.newDiscountType === 1 && (v < 0 || v > 100)) return 'النسبة يجب أن تكون بين 0 و 100.';
    if (this.newDiscountType === 2 && v < 0) return 'مبلغ الخصم لا يمكن أن يكون سالباً.';
    if (this.newDiscountType === 2 && v > this.computedSubTotal) return 'الخصم أكبر من إجمالي الفاتورة.';
    return '';
  }

  onDiscountChange(): void {
    if (this.newPaymentType === 1) {
      this.newInitialPayment = this.computedTotal;
    }
  }

  /** Net payable = subtotal - discount. Cash payment and balances are based on this. */
  get computedTotal(): number {
    return Math.round((this.computedSubTotal - this.computedDiscount) * 100) / 100;
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

    if (this.newDiscountError) {
      this.invoiceError = this.newDiscountError;
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
      discountType: this.newDiscountType,
      discountValue: this.newDiscountType ? Number(this.newDiscountValue) || 0 : undefined,
      initialPayment: initialPaymentAmount,
      initialPaymentMethod: isCash ? (this.newInitialPaymentMethod || 'نقدي') : (initialPaymentAmount && initialPaymentAmount > 0 ? this.newInitialPaymentMethod : undefined),
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
    this.newDiscountType = 0;
    this.newDiscountValue = 0;
    this.invoiceItems = [
      { rmsItemID: 0, description: '', quantity: 1, unitPrice: 0 }
    ];
    this.calculateDueDate();
  }

  viewInvoice(id: number): void {
    this.router.navigate(['/invoices', id]);
  }

  toggleExpandInvoice(id: number, event?: Event): void {
    if (event) event.stopPropagation();
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
        this.successMessage = 'تم تسجيل دفعة بقيمة ' + req.amount + ' ج.م بنجاح!';
        this.toast.success(this.successMessage);
        if (this.activeSection === 'returns') {
          this.fetchReturns(false);
        } else {
          this.fetchInvoices(false);
        }
        setTimeout(() => this.successMessage = '', 5000);
      },
      error: (err) => {
        this.processingPayment = false;
        console.error('Error recording payment', err);
        this.toast.error(err.error?.message || 'فشل في تسجيل الدفعة.');
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/suppliers']);
  }
}
