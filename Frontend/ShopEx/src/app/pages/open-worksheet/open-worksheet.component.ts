import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { StockService } from '../../services/stock.service';
import { ToastService } from '../../services/toast.service';
import { StoreOption, StockBalance } from '../../models/models';
import { TabbedSelectComponent, TabbedSelectItem } from '../../components/tabbed-select/tabbed-select.component';

interface TransferLine {
  itemId: number;
  description: string;
  qty: number;
  available: number;
  price: number;
}

@Component({
  selector: 'app-open-worksheet',
  standalone: true,
  imports: [CommonModule, FormsModule, TabbedSelectComponent],
  templateUrl: './open-worksheet.component.html'
})
export class OpenWorksheetComponent implements OnInit {
  stores: StoreOption[] = [];
  storeId: number | null = null;
  notes: string = '';

  stock: StockBalance[] = [];
  loadingStock: boolean = false;

  lines: TransferLine[] = [this.emptyLine()];

  submitting: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  constructor(
    private api: ApiService,
    private stockService: StockService,
    private toast: ToastService
  ) { }

  ngOnInit(): void {
    this.api.getStores().subscribe({
      next: (data) => this.stores = data || [],
      error: () => this.toast.error('تعذر تحميل قائمة الفروع')
    });
    this.loadStock();
  }

  private emptyLine(): TransferLine {
    return { itemId: 0, description: '', qty: 1, available: 0, price: 0 };
  }

  private loadStock(): void {
    this.loadingStock = true;
    this.stockService.getWarehouses(true).subscribe({
      next: (whs) => {
        const wh = (whs || []).find(w => w.isDefault) || (whs || [])[0];
        if (!wh) {
          this.loadingStock = false;
          this.errorMessage = 'لا يوجد مخزن مفعّل.';
          return;
        }
        this.stockService.getAllBalances(wh.id).subscribe({
          next: (b) => {
            this.stock = (b || []).filter(x => x.quantityOnHand > 0);
            this.loadingStock = false;
          },
          error: () => {
            this.loadingStock = false;
            this.toast.error('تعذر تحميل أرصدة المخزن');
          }
        });
      },
      error: () => {
        this.loadingStock = false;
        this.toast.error('تعذر تحميل المخازن');
      }
    });
  }

  /** Stock items not already picked in another row. */
  selectableFor(index: number): TabbedSelectItem[] {
    const taken = new Set(this.lines.filter((_, i) => i !== index).map(l => l.itemId));
    return this.stock
      .filter(s => !taken.has(s.rmsItemID))
      .map(s => ({
        id: s.rmsItemID,
        title: s.itemDescription || `صنف #${s.rmsItemID}`,
        subtitle: `${s.itemLookupCode ? 'باركود: ' + s.itemLookupCode + ' | ' : ''}المتاح: ${s.quantityOnHand}`,
        quantity: s.quantityOnHand,
        icon: 'inventory_2',
        raw: s
      }));
  }

  onItemSelect(index: number, selected: TabbedSelectItem): void {
    const s = selected.raw as StockBalance;
    const line = this.lines[index];
    line.itemId = s.rmsItemID;
    line.description = s.itemDescription || selected.title;
    line.available = s.quantityOnHand;
    line.price = s.averageCost || s.cost || 0;
    if (!line.qty || line.qty < 1) line.qty = 1;
  }

  onItemClear(index: number): void {
    this.lines[index] = this.emptyLine();
  }

  addLine(): void {
    this.lines.push(this.emptyLine());
  }

  removeLine(index: number): void {
    if (this.lines.length > 1) this.lines.splice(index, 1);
    else this.lines[0] = this.emptyLine();
  }

  overStock(l: TransferLine): boolean {
    return l.itemId > 0 && Number(l.qty) > l.available;
  }

  get filledLines(): TransferLine[] {
    return this.lines.filter(l => l.itemId > 0);
  }

  get totalQty(): number {
    return this.filledLines.reduce((s, l) => s + (Number(l.qty) || 0), 0);
  }

  get storeName(): string {
    return this.stores.find(s => s.storeId === this.storeId)?.storeName || '';
  }

  get canSubmit(): boolean {
    return !!this.storeId && this.filledLines.length > 0 && this.lines.every(l =>
      l.itemId === 0 || (Number(l.qty) > 0 && !this.overStock(l)));
  }

  approve(): void {
    this.errorMessage = '';
    this.successMessage = '';
    if (!this.canSubmit) {
      this.errorMessage = 'اختر الفرع، ثم أضف أصنافًا بكميات صحيحة لا تتجاوز المتاح.';
      this.toast.error(this.errorMessage);
      return;
    }

    this.submitting = true;
    this.api.transferWorksheet({
      title: `Worksheet - Purchase Order`,
      notes: this.notes?.trim() || undefined,
      storeId: Number(this.storeId),
      items: this.filledLines.map(l => ({
        itemId: l.itemId,
        description: l.description,
        qty: Number(l.qty),
        price: Number(l.price),
        storeId: Number(this.storeId),
        taxRate: 0
      }))
    }).subscribe({
      next: (res) => {
        this.submitting = false;
        this.successMessage = `تم اعتماد أمر التحويل #${res.worksheetId} إلى ${this.storeName} (الحالة 2).`;
        this.toast.success(this.successMessage);
        this.lines = [this.emptyLine()];
        this.notes = '';
        this.loadStock();
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = err.error?.message || err.message || 'فشل اعتماد أمر التحويل.';
        this.toast.error(this.errorMessage);
      }
    });
  }
}
