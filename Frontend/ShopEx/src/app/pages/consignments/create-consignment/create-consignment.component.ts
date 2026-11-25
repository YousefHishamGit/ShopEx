import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ConsignmentService, AGREED_DAYS_OPTIONS } from '../../../services/consignment.service';
import { StockService } from '../../../services/stock.service';
import { ToastService } from '../../../services/toast.service';
import {
  Dealer,
  Warehouse,
  StockBalance,
  CreateConsignmentRequest,
  CreateConsignmentItemRequest,
  CreateDealerRequest
} from '../../../models/models';
import { TabbedSelectComponent, TabbedSelectItem } from '../../../components/tabbed-select/tabbed-select.component';

type ItemRow = CreateConsignmentItemRequest & { editPrice?: boolean };

@Component({
  selector: 'app-create-consignment',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TabbedSelectComponent],
  templateUrl: './create-consignment.component.html'
})
export class CreateConsignmentComponent implements OnInit {
  agreedDaysOptions = AGREED_DAYS_OPTIONS;
  dealers: Dealer[] = [];
  warehouses: Warehouse[] = [];
  stockBalances: StockBalance[] = [];
  loadingBalances = false;

  selectedDealerId: number | null = null;
  selectedWarehouseId: number | null = null;
  agreedDays: number = 15;
  dueDate: string = '';
  notes: string = '';

  items: ItemRow[] = [
    {
      rmsItemID: 0,
      quantityIssued: 1,
      unitPrice: 0,
      unitCost: 0
    }
  ];

  submitting = false;
  errorMessage = '';

  // Quick New Dealer Modal
  showQuickDealerModal = false;
  savingQuickDealer = false;
  quickDealerError = '';
  quickDealer: CreateDealerRequest = {
    name: '',
    phone: '',
    address: '',
    notes: ''
  };

  constructor(
    private consignmentService: ConsignmentService,
    private stockService: StockService,
    private toast: ToastService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.calculateDueDate();
    this.loadDealers();
    this.loadWarehouses();
  }

  calculateDueDate(): void {
    const d = new Date();
    d.setDate(d.getDate() + (Number(this.agreedDays) >= 0 ? Number(this.agreedDays) : 15));
    // local date (toISOString would shift the day around midnight for UTC+2/+3)
    const pad = (n: number) => String(n).padStart(2, '0');
    this.dueDate = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  }

  onAgreedDaysChange(): void {
    this.calculateDueDate();
  }

  loadDealers(): void {
    this.consignmentService.getDealers(true).subscribe({
      next: (data) => {
        this.dealers = data || [];
        if (this.dealers.length > 0 && !this.selectedDealerId) {
          this.selectedDealerId = this.dealers[0].id;
        }
      },
      error: (err) => console.error('Error loading dealers:', err)
    });
  }

  get selectableDealers(): TabbedSelectItem[] {
    return this.dealers.map(d => ({
      id: d.id,
      title: d.name,
      subtitle: d.phone ? `هاتف: ${d.phone}` : `بنزينة #${d.id}`,
      category: d.address || 'عام',
      icon: 'local_gas_station',
      raw: d
    }));
  }

  loadWarehouses(): void {
    this.stockService.getWarehouses(true).subscribe({
      next: (data) => {
        this.warehouses = data || [];
        if (this.warehouses.length > 0) {
          const defaultWh = this.warehouses.find((w) => w.isDefault) || this.warehouses[0];
          this.selectedWarehouseId = defaultWh.id;
          this.loadWarehouseBalances();
        }
      },
      error: (err) => console.error('Error loading warehouses:', err)
    });
  }

  onWarehouseChange(): void {
    // prices / availability are per warehouse: clear picked items so nothing stale gets submitted
    if (this.items.some((i) => i.rmsItemID > 0)) {
      this.items = [{ rmsItemID: 0, quantityIssued: 1, unitPrice: 0, unitCost: 0 }];
      this.toast.info('تم تفريغ الأصناف لتغيير المخزن، يرجى اختيارها من جديد.');
    }
    this.stockBalances = [];
    this.loadWarehouseBalances();
  }

  /** Quantity on hand of an item in the selected warehouse (null when unknown). */
  availableFor(rmsItemID: number): number | null {
    if (!rmsItemID) return null;
    const b = this.stockBalances.find((x) => x.rmsItemID === rmsItemID);
    return b ? b.quantityOnHand : null;
  }

  /** Total quantity requested for an item across all lines. */
  requestedFor(rmsItemID: number): number {
    return this.items
      .filter((i) => i.rmsItemID === rmsItemID)
      .reduce((s, i) => s + (Number(i.quantityIssued) || 0), 0);
  }

  isOverStock(item: CreateConsignmentItemRequest): boolean {
    const avail = this.availableFor(item.rmsItemID);
    return avail !== null && this.requestedFor(item.rmsItemID) > avail;
  }

  isDuplicate(idx: number): boolean {
    const id = this.items[idx].rmsItemID;
    return id > 0 && this.items.findIndex((i) => i.rmsItemID === id) !== idx;
  }

  trackByIndex(index: number): number {
    return index;
  }

  loadWarehouseBalances(): void {
    if (!this.selectedWarehouseId) return;
    this.loadingBalances = true;
    this.stockService.getAllBalances(this.selectedWarehouseId).subscribe({
      next: (balances) => {
        this.stockBalances = balances || [];
        this.loadingBalances = false;
      },
      error: () => {
        this.loadingBalances = false;
      }
    });
  }

  get selectableStockItems(): TabbedSelectItem[] {
    return this.stockBalances.map(b => ({
      id: b.rmsItemID,
      title: b.itemDescription || `صنف #${b.rmsItemID}`,
      subtitle: b.itemLookupCode ? `باركود: ${b.itemLookupCode} | رصيد بالمخزن: ${b.quantityOnHand} قطعة` : `رصيد متاح بالمخزن: ${b.quantityOnHand} قطعة`,
      cost: b.cost || b.averageCost || 0,
      price: (b.retailPrice && b.retailPrice > 0) ? b.retailPrice : (b.cost || b.averageCost ? Math.round((b.cost || b.averageCost) * 1.25 * 100) / 100 : 0),
      quantity: b.quantityOnHand,
      category: b.quantityOnHand > 0 ? 'متوفر بالمخزن' : 'رصيد صفري',
      badge: `${b.quantityOnHand} في المخزن`,
      icon: 'inventory_2',
      raw: b
    }));
  }

  onStockItemSelect(index: number, selected: TabbedSelectItem): void {
    const raw = selected.raw as StockBalance;
    this.items[index].rmsItemID = Number(selected.id);
    // Real price and cost from RMS Database!
    this.items[index].unitCost = (raw && raw.cost) ? raw.cost : (selected.cost || 0);
    this.items[index].unitPrice = (raw && raw.retailPrice && raw.retailPrice > 0) ? raw.retailPrice : (selected.price || 0);
  }

  onStockItemClear(index: number): void {
    this.items[index].rmsItemID = 0;
    this.items[index].unitPrice = 0;
    this.items[index].unitCost = 0;
  }

  setAgreedDays(days: number): void {
    this.agreedDays = days;
    this.calculateDueDate();
  }

  stepQuantity(item: ItemRow, delta: number): void {
    const avail = this.availableFor(item.rmsItemID);
    let next = (Number(item.quantityIssued) || 0) + delta;
    if (avail !== null) next = Math.min(next, avail);
    item.quantityIssued = Math.max(1, next);
  }

  fillMaxQuantity(item: ItemRow): void {
    const avail = this.availableFor(item.rmsItemID);
    if (avail !== null && avail > 0) item.quantityIssued = avail;
  }

  addItem(): void {
    this.items.push({
      rmsItemID: 0,
      quantityIssued: 1,
      unitPrice: 0,
      unitCost: 0
    });
  }

  removeItem(index: number): void {
    if (this.items.length > 1) {
      this.items.splice(index, 1);
    }
  }

  get totalQuantity(): number {
    return this.items.reduce((sum, i) => sum + (Number(i.quantityIssued) || 0), 0);
  }

  get totalIssuedValue(): number {
    return this.items.reduce(
      (sum, i) => sum + (Number(i.quantityIssued) || 0) * (Number(i.unitPrice) || 0),
      0
    );
  }

  get totalCostValue(): number {
    return this.items.reduce(
      (sum, i) => sum + (Number(i.quantityIssued) || 0) * (Number(i.unitCost) || 0),
      0
    );
  }

  get totalExpectedProfit(): number {
    return this.totalIssuedValue - this.totalCostValue;
  }

  submitConsignment(): void {
    if (!this.selectedDealerId) {
      this.toast.error('يرجى اختيار البنزينة / المحل.');
      return;
    }

    if (!this.items || this.items.length === 0) {
      this.toast.error('يرجى إضافة صنف واحد على الأقل.');
      return;
    }

    for (let idx = 0; idx < this.items.length; idx++) {
      const item = this.items[idx];
      if (!item.rmsItemID || item.rmsItemID <= 0) {
        this.toast.error(`السطر رقم (${idx + 1}): يرجى اختيار صنف صحيح.`);
        return;
      }
      if (!item.quantityIssued || item.quantityIssued <= 0) {
        this.toast.error(`السطر رقم (${idx + 1}): كمية الصرف يجب أن تكون أكبر من صفر.`);
        return;
      }
      if (item.unitPrice < 0) {
        this.toast.error(`السطر رقم (${idx + 1}): سعر البيع لا يمكن أن يكون سالباً.`);
        return;
      }
      if (item.unitCost < 0) {
        this.toast.error(`السطر رقم (${idx + 1}): سعر التكلفة لا يمكن أن يكون سالباً.`);
        return;
      }
    }

    const seen = new Set<number>();
    for (let idx = 0; idx < this.items.length; idx++) {
      const item = this.items[idx];
      if (seen.has(item.rmsItemID)) {
        this.toast.error(`السطر رقم (${idx + 1}): الصنف مكرر، يرجى دمج الكميات في سطر واحد.`);
        return;
      }
      seen.add(item.rmsItemID);
      if (this.isOverStock(item)) {
        this.toast.error(
          `السطر رقم (${idx + 1}): الكمية المطلوبة تتجاوز الرصيد المتاح بالمخزن (${this.availableFor(item.rmsItemID)}).`
        );
        return;
      }
    }

    if (this.dueDate && new Date(this.dueDate) < new Date(new Date().toDateString())) {
      this.toast.error('تاريخ الاستحقاق لا يمكن أن يكون في الماضي.');
      return;
    }

    const request: CreateConsignmentRequest = {
      dealerID: this.selectedDealerId,
      warehouseID: this.selectedWarehouseId || undefined,
      agreedDays: Number(this.agreedDays) || 15,
      dueDate: this.dueDate || undefined,
      notes: this.notes.trim() || undefined,
      items: this.items.map((i) => ({
        rmsItemID: Number(i.rmsItemID),
        quantityIssued: Number(i.quantityIssued),
        unitPrice: Number(i.unitPrice),
        unitCost: Number(i.unitCost)
      }))
    };

    this.submitting = true;
    this.errorMessage = '';

    this.consignmentService.createConsignment(request).subscribe({
      next: (res) => {
        this.submitting = false;
        this.toast.success(`تم حفظ إذن الأمانة ${res.consignmentNumber} بنجاح.`);
        // print=1: صفحة الإذن تطبع نفسها فور فتحها (صرف البضاعة = طباعة الإذن على طول)
        this.router.navigate(['/consignments', res.id], { queryParams: { print: 1 } });
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = err.error?.message || 'حدث خطأ أثناء حفظ إذن صرف البنزينة.';
        this.toast.error(this.errorMessage);
      }
    });
  }

  openQuickDealerModal(): void {
    this.quickDealer = { name: '', phone: '', address: '', notes: '' };
    this.quickDealerError = '';
    this.showQuickDealerModal = true;
  }

  closeQuickDealerModal(): void {
    this.showQuickDealerModal = false;
  }

  saveQuickDealer(): void {
    if (!this.quickDealer.name || !this.quickDealer.name.trim()) {
      this.quickDealerError = 'يرجى كتابة اسم البنزينة / المحل.';
      return;
    }
    if (!this.quickDealer.phone || !this.quickDealer.phone.trim()) {
      this.quickDealerError = 'يرجى كتابة رقم الهاتف.';
      return;
    }

    this.savingQuickDealer = true;
    this.quickDealerError = '';
    this.consignmentService.createDealer(this.quickDealer).subscribe({
      next: (created) => {
        this.dealers.push(created);
        this.selectedDealerId = created.id;
        this.savingQuickDealer = false;
        this.closeQuickDealerModal();
      },
      error: (err) => {
        this.quickDealerError = err.error?.message || 'حدث خطأ أثناء حفظ البنزينة.';
        this.savingQuickDealer = false;
      }
    });
  }
}
