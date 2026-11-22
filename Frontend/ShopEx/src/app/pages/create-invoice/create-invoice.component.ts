import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { StockService } from '../../services/stock.service';
import { ToastService } from '../../services/toast.service';
import { Supplier, SupplierItem, CreateInvoiceRequest, CreateInvoiceItemRequest, Warehouse } from '../../models/models';
import { TabbedSelectComponent, TabbedSelectItem } from '../../components/tabbed-select/tabbed-select.component';

export interface ExtendedInvoiceItem extends CreateInvoiceItemRequest {
  dbCost?: number;
  dbPrice?: number;
  isLoadingDb?: boolean;
}

@Component({
  selector: 'app-create-invoice',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TabbedSelectComponent],
  templateUrl: './create-invoice.component.html'
})
export class CreateInvoiceComponent implements OnInit {
  suppliers: Supplier[] = [];
  warehouses: Warehouse[] = [];
  supplierItems: SupplierItem[] = [];
  generalItems: SupplierItem[] = [];
  loadingSupplierItems: boolean = false;

  selectedSupplierId: number | null = null;
  selectedWarehouseId: number | null = null;
  invoiceNumber: string = '';
  invoiceDate: string = new Date().toISOString().substring(0, 10);
  paymentType: number = 2; // 1 = Cash, 2 = Credit
  creditDays: number = 30;
  notes: string = '';
  initialPayment: number = 0;
  discountType: number = 0;   // 0 none, 1 percent, 2 amount
  discountValue: number = 0;
  initialPaymentMethod: string = 'نقدي';

  items: ExtendedInvoiceItem[] = [];

  submitting: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  constructor(
    private api: ApiService,
    private stockService: StockService,
    private toast: ToastService,
    private router: Router
  ) {
    const navigation = this.router.getCurrentNavigation();
    if (navigation?.extras?.state) {
      const state = navigation.extras.state as { supplierId?: number; items?: SupplierItem[] };
      if (state.supplierId) {
        this.selectedSupplierId = state.supplierId;
      }
      if (state.items && state.items.length > 0) {
        this.items = state.items.map(i => ({
          itemId: i.id,
          description: i.description,
          quantity: 1,
          unitPrice: (i.cost && i.cost > 0) ? i.cost : (i.price || 0),
          dbCost: i.cost,
          dbPrice: i.price,
          taxRate: i.taxRate || 0
        }));
      }
    }
  }

  ngOnInit(): void {
    if (this.items.length === 0) {
      this.addItemRow();
    }

    this.stockService.getWarehouses(true).subscribe({
      next: (data) => {
        this.warehouses = data || [];
        if (this.warehouses.length > 0 && !this.selectedWarehouseId) {
          const defaultWh = this.warehouses.find(w => w.isDefault) || this.warehouses[0];
          this.selectedWarehouseId = defaultWh.id;
        }
      }
    });

    this.api.getAllSuppliers().subscribe({
      next: (data) => {
        this.suppliers = data || [];
        if (!this.selectedSupplierId && this.suppliers.length > 0) {
          this.selectedSupplierId = this.suppliers[0].id;
        }
        if (this.selectedSupplierId) {
          this.loadSupplierItems();
        }
      },
      error: (err) => console.error('Failed to load suppliers for dropdown', err)
    });

    // Also load general items from DB as fallback
    this.api.searchItems('', 100).subscribe({
      next: (data) => {
        this.generalItems = data || [];
      }
    });
  }

  get selectableSuppliers(): TabbedSelectItem[] {
    return this.suppliers.map(s => ({
      id: s.id,
      title: s.supplierName,
      subtitle: s.code ? `كود: ${s.code} | حساب: ${s.accountNumber || '—'}` : `مورد #${s.id}`,
      category: s.city || 'عام',
      badge: s.phone || undefined,
      icon: 'storefront',
      raw: s
    }));
  }

  onTabbedSupplierSelect(item: TabbedSelectItem): void {
    this.selectedSupplierId = Number(item.id);
    this.loadSupplierItems();
  }

  onTabbedSupplierClear(): void {
    this.selectedSupplierId = null;
    this.supplierItems = [];
  }

  loadSupplierItems(): void {
    if (!this.selectedSupplierId) return;
    this.loadingSupplierItems = true;
    this.api.getSupplierItems(this.selectedSupplierId).subscribe({
      next: (items) => {
        this.supplierItems = items || [];
        this.loadingSupplierItems = false;
      },
      error: (err) => {
        console.error(err);
        this.loadingSupplierItems = false;
        this.toast.error('تعذر جلب أصناف هذا المورد');
      }
    });
  }

  get activeItemsList(): SupplierItem[] {
    return this.supplierItems.length > 0 ? this.supplierItems : this.generalItems;
  }

  get selectableItems(): TabbedSelectItem[] {
    return this.activeItemsList.map(item => ({
      id: item.id,
      title: item.description,
      subtitle: item.itemLookupCode ? `باركود: ${item.itemLookupCode} | تكلفة DB: ${item.cost || 0} ج.م` : `صنف #${item.id} | تكلفة DB: ${item.cost || 0} ج.م`,
      price: (item.cost && item.cost > 0) ? item.cost : (item.price || 0),
      cost: item.cost,
      quantity: item.quantity,
      category: (item.cost && item.cost > 0) ? 'أصناف بتكلفة مسجلة' : 'أصناف عامة',
      badge: (item.cost && item.cost > 0) ? `تكلفة: ${item.cost} ج.م` : (item.price ? `بيع: ${item.price} ج.م` : undefined),
      icon: 'inventory_2',
      raw: item
    }));
  }

  onTabbedItemSelect(index: number, selected: TabbedSelectItem): void {
    const it = selected.raw || this.activeItemsList.find(i => i.id === Number(selected.id));
    if (it) {
      this.items[index].itemId = it.id;
      this.items[index].description = it.description || selected.title;
      const dbCost = it.cost || 0;
      const dbPrice = it.price || 0;
      this.items[index].dbCost = dbCost;
      this.items[index].dbPrice = dbPrice;
      // Price & Cost returned directly from the database!
      this.items[index].unitPrice = dbCost > 0 ? dbCost : (dbPrice > 0 ? dbPrice : Number(selected.price || 0));
      this.items[index].taxRate = it.taxRate || 0;
    } else {
      this.items[index].itemId = Number(selected.id);
      this.items[index].description = selected.title;
      this.items[index].unitPrice = Number(selected.price || 0);
    }
  }

  onTabbedItemClear(index: number): void {
    this.items[index].itemId = 0;
    this.items[index].description = '';
    this.items[index].unitPrice = 0;
    this.items[index].dbCost = 0;
    this.items[index].dbPrice = 0;
  }

  onManualItemLookup(index: number): void {
    const id = this.items[index].itemId;
    if (!id || id <= 0) return;

    this.items[index].isLoadingDb = true;
    this.api.getItemById(id).subscribe({
      next: (item) => {
        this.items[index].isLoadingDb = false;
        if (item) {
          this.items[index].description = item.description;
          const dbCost = item.cost || 0;
          const dbPrice = item.price || 0;
          this.items[index].dbCost = dbCost;
          this.items[index].dbPrice = dbPrice;
          this.items[index].unitPrice = dbCost > 0 ? dbCost : (dbPrice > 0 ? dbPrice : 0);
          this.toast.info(`تم جلب سعر وتكلفة الصنف (${item.description}) من قاعدة البيانات تلقائياً.`);
        }
      },
      error: () => {
        this.items[index].isLoadingDb = false;
      }
    });
  }

  addItemRow(): void {
    this.items.push({
      itemId: 0,
      description: '',
      quantity: 1,
      unitPrice: 0,
      taxRate: 0,
      dbCost: 0,
      dbPrice: 0
    });
  }

  removeItemRow(index: number): void {
    if (this.items.length > 1) {
      this.items.splice(index, 1);
    }
  }

  /** Sum of item lines before discount (the server does not apply the tax column). */
  get subTotal(): number {
    return this.items.reduce((sum, item) => sum + (item.quantity || 0) * (item.unitPrice || 0), 0);
  }

  get discountAmount(): number {
    const sub = this.subTotal;
    const v = Number(this.discountValue) || 0;
    if (this.discountType === 1) return Math.min(sub, Math.round(sub * Math.min(v, 100)) / 100);
    if (this.discountType === 2) return Math.min(sub, v);
    return 0;
  }

  get discountError(): string {
    const v = Number(this.discountValue) || 0;
    if (this.discountType === 1 && (v < 0 || v > 100)) return 'النسبة يجب أن تكون بين 0 و 100.';
    if (this.discountType === 2 && v < 0) return 'مبلغ الخصم لا يمكن أن يكون سالباً.';
    if (this.discountType === 2 && v > this.subTotal) return 'الخصم أكبر من إجمالي الفاتورة.';
    return '';
  }

  onDiscountChange(): void { /* totals are getters; nothing to recompute */ }

  /** Net payable = subtotal - discount. */
  get totalAmount(): number {
    return Math.round((this.subTotal - this.discountAmount) * 100) / 100;
  }

  generateInvoiceNumber(): void {
    const ts = Date.now().toString(36).toUpperCase();
    this.invoiceNumber = `INV-${ts}`;
  }

  submitInvoice(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.invoiceNumber.trim()) {
      this.generateInvoiceNumber();
    }

    if (!this.selectedSupplierId) {
      this.errorMessage = 'يرجى اختيار المورد أولاً.';
      this.toast.error(this.errorMessage);
      return;
    }
    if (this.paymentType === 2 && (this.creditDays === null || this.creditDays < 0)) {
      this.errorMessage = 'يرجى تحديد عدد أيام الأجل للفاتورة الآجلة.';
      this.toast.error(this.errorMessage);
      return;
    }
    if (this.items.length === 0 || this.items.some(i => !i.itemId || i.quantity <= 0)) {
      this.errorMessage = 'يجب أن يحتوي كل بند على صنف صالح وكمية أكبر من صفر.';
      this.toast.error(this.errorMessage);
      return;
    }

    if (this.discountError) {
      this.errorMessage = this.discountError;
      this.toast.error(this.errorMessage);
      return;
    }

    const isCash = this.paymentType === 1;
    const initialPaymentAmount = isCash ? this.totalAmount : (this.initialPayment > 0 ? this.initialPayment : undefined);

    const payload: CreateInvoiceRequest = {
      supplierId: this.selectedSupplierId,
      warehouseID: this.selectedWarehouseId || undefined,
      invoiceNumber: this.invoiceNumber.trim(),
      invoiceDate: this.invoiceDate,
      paymentType: this.paymentType,
      creditDays: isCash ? undefined : (this.creditDays > 0 ? this.creditDays : undefined),
      notes: this.notes.trim(),
      discountType: this.discountType,
      discountValue: this.discountType ? Number(this.discountValue) || 0 : undefined,
      initialPayment: initialPaymentAmount,
      initialPaymentMethod: isCash ? (this.initialPaymentMethod || 'نقدي') : (this.initialPayment > 0 ? this.initialPaymentMethod : undefined),
      items: this.items.map(i => ({
        itemId: Number(i.itemId),
        description: i.description?.trim(),
        quantity: Number(i.quantity),
        unitPrice: Number(i.unitPrice),
        taxRate: Number(i.taxRate || 0)
      }))
    };

    this.submitting = true;
    this.api.createInvoice(payload).subscribe({
      next: (res) => {
        this.submitting = false;
        this.successMessage = `تم إنشاء الفاتورة رقم ${res.invoiceNumber} بنجاح وترحيلها لحساب المورد!`;
        this.toast.success(this.successMessage);
        setTimeout(() => {
          this.router.navigate(['/invoices', res.id], { queryParams: { print: 'true' } });
        }, 800);
      },
      error: (err) => {
        this.submitting = false;
        console.error('Error creating invoice', err);
        this.errorMessage = err.error?.message || err.message || 'فشل في حفظ الفاتورة.';
        this.toast.error(this.errorMessage);
      }
    });
  }
}
