import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { DeviceService } from '../../services/device.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { ThermalReceiptService } from '../../shared/print/thermal-receipt.service';
import {
  DeviceFilters,
  DeviceResponse,
  DeviceStatus,
  DeviceSummaryDto
} from '../../models/models';

/**
 * أجهزة (موبايلات) مشتراة بغرض البيع. صفحة واحدة للأدمن (/devices) ولموظف الفرع (/branch/devices):
 * الكل يرى كل الأجهزة، لكن الموظف يسجّل ويعدّل ويبيع أجهزة فرعه فقط.
 */
@Component({
  selector: 'app-devices',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './devices.component.html'
})
export class DevicesComponent implements OnInit, OnDestroy {
  readonly DeviceStatus = DeviceStatus;
  /** سعات التخزين الثابتة */
  readonly storageOptions = ['16GB', '32GB', '64GB', '128GB', '256GB', '512GB', '1TB', '2TB'];

  stores: { id: number; name: string }[] = [];
  devices: DeviceResponse[] = [];
  summary: DeviceSummaryDto | null = null;

  // Filters + paging
  search = '';
  statusFilter: number | '' = '';
  storeFilter: number | '' = '';
  fromDate = '';
  toDate = '';
  page = 1;
  readonly pageSize = 25;

  loading = false;
  errorMessage = '';
  private loadSub?: Subscription;
  private searchTimer?: ReturnType<typeof setTimeout>;

  // Add / edit modal
  showFormModal = false;
  editing: DeviceResponse | null = null;
  submitting = false;
  formError = '';
  form = this.emptyForm();

  // Sell modal
  sellTarget: DeviceResponse | null = null;
  sellForm = { salePrice: null as number | null, soldStoreId: null as number | null, buyerName: '', buyerPhone: '', notes: '' };
  sellError = '';

  // Confirm modal (cancel sale / delete)
  confirmAction: { kind: 'cancel-sale' | 'delete'; device: DeviceResponse } | null = null;
  confirmError = '';

  constructor(
    private deviceService: DeviceService,
    public auth: AuthService,
    private toast: ToastService,
    private receipt: ThermalReceiptService
  ) { }

  get isAdmin(): boolean {
    return this.auth.isAdmin();
  }

  get ownStoreId(): number | null {
    return this.auth.currentUser()?.storeId ?? null;
  }

  ngOnInit(): void {
    this.deviceService.getStores().subscribe({ next: (s) => (this.stores = s || []), error: () => { } });
    this.load();
  }

  ngOnDestroy(): void {
    this.loadSub?.unsubscribe();
    clearTimeout(this.searchTimer);
  }

  /** أي موظف يقدر يبيع أي جهاز متاح (اللي يجيب أحسن سعر يبيعه) */
  canSell(d: DeviceResponse): boolean {
    return d.status === DeviceStatus.InStock;
  }

  /** الأدمن يعدّل كل شيء، والموظف أجهزة فرعه المتاحة فقط */
  canModify(d: DeviceResponse): boolean {
    return this.isAdmin || (d.storeId !== null && d.storeId === this.ownStoreId);
  }

  canEdit(d: DeviceResponse): boolean {
    return this.isAdmin || (this.canModify(d) && d.status === DeviceStatus.InStock);
  }

  // ---------- Load ----------
  private filters(): DeviceFilters {
    return {
      search: this.search,
      status: this.statusFilter === '' ? undefined : Number(this.statusFilter),
      storeId: this.storeFilter === '' ? undefined : Number(this.storeFilter),
      fromDate: this.fromDate || undefined,
      toDate: this.toDate || undefined
    };
  }

  load(): void {
    this.loadSub?.unsubscribe();
    this.loading = true;
    this.errorMessage = '';

    this.loadSub = this.deviceService.getAll(this.filters(), this.page, this.pageSize).subscribe({
      next: (s) => {
        this.summary = s;
        this.devices = s.items;
        this.page = s.page;
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'تعذر تحميل الأجهزة.';
        this.loading = false;
      }
    });
  }

  onFilterChange(): void {
    if (this.fromDate && this.toDate && this.fromDate > this.toDate) return;
    this.page = 1;
    this.load();
  }

  onSearchInput(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.onFilterChange(), 350);
  }

  resetFilters(): void {
    this.search = '';
    this.statusFilter = '';
    this.storeFilter = '';
    this.fromDate = this.toDate = '';
    this.onFilterChange();
  }

  goToPage(p: number): void {
    const total = this.summary?.totalPages ?? 1;
    if (p < 1 || p > total || p === this.page) return;
    this.page = p;
    this.load();
  }

  get pageFrom(): number {
    return this.summary && this.summary.stats.totalCount > 0 ? (this.page - 1) * this.pageSize + 1 : 0;
  }

  get pageTo(): number {
    return this.summary ? Math.min(this.page * this.pageSize, this.summary.stats.totalCount) : 0;
  }

  // ---------- Add / edit ----------
  openCreate(): void {
    this.editing = null;
    this.form = this.emptyForm();
    this.formError = '';
    this.showFormModal = true;
  }

  openEdit(d: DeviceResponse): void {
    this.editing = d;
    this.form = {
      storeId: d.storeId,
      brand: d.brand,
      model: d.model,
      color: d.color || '',
      storage: d.storage || '',
      imei: d.imei || '',
      batteryHealth: d.batteryHealth ?? null,
      hasBox: d.hasBox,
      purchasePrice: d.purchasePrice,
      purchaseDate: d.purchaseDate ? d.purchaseDate.substring(0, 10) : '',
      sellerName: d.sellerName || '',
      notes: d.notes || ''
    };
    this.formError = '';
    this.showFormModal = true;
  }

  closeFormModal(): void {
    if (this.submitting) return;
    this.showFormModal = false;
  }

  saveDevice(): void {
    if (this.submitting) return;

    const price = Number(this.form.purchasePrice);
    if (!this.form.brand.trim() || !this.form.model.trim()) {
      this.formError = 'يرجى إدخال ماركة وموديل الجهاز.';
      return;
    }
    if (!price || price <= 0) {
      this.formError = 'يرجى إدخال التكلفة (أكبر من صفر).';
      return;
    }

    const body = {
      brand: this.form.brand.trim(),
      model: this.form.model.trim(),
      color: this.form.color.trim() || undefined,
      storage: this.form.storage.trim() || undefined,
      imei: this.form.imei.trim() || undefined,
      batteryHealth: this.form.batteryHealth === null || (this.form.batteryHealth as unknown) === '' ? undefined : Number(this.form.batteryHealth),
      hasBox: this.form.hasBox,
      purchasePrice: price,
      purchaseDate: this.form.purchaseDate || undefined,
      sellerName: this.form.sellerName.trim() || undefined,
      notes: this.form.notes.trim() || undefined
    };

    this.submitting = true;
    this.formError = '';

    const isEdit = !!this.editing;
    const request$ = this.editing
      ? this.deviceService.update(this.editing.id, body)
      : this.deviceService.create({ ...body, storeId: this.isAdmin ? this.form.storeId : undefined });

    request$.subscribe({
      next: () => {
        this.submitting = false;
        this.showFormModal = false;
        this.toast.success(isEdit ? 'تم تعديل بيانات الجهاز.' : 'تم تسجيل شراء الجهاز.');
        if (!isEdit) this.page = 1;
        this.load();
      },
      error: (err) => {
        this.submitting = false;
        this.formError = err?.error?.message || 'تعذر حفظ الجهاز.';
      }
    });
  }

  // ---------- Sell ----------
  openSell(d: DeviceResponse): void {
    this.sellTarget = d;
    this.sellForm = { salePrice: null, soldStoreId: null, buyerName: '', buyerPhone: '', notes: '' };
    this.sellError = '';
  }

  closeSell(): void {
    if (this.submitting) return;
    this.sellTarget = null;
  }

  get sellProfit(): number | null {
    const price = Number(this.sellForm.salePrice);
    return this.sellTarget && price > 0 ? price - this.sellTarget.purchasePrice : null;
  }

  confirmSell(): void {
    if (this.submitting || !this.sellTarget) return;

    const price = Number(this.sellForm.salePrice);
    if (!price || price <= 0) {
      this.sellError = 'يرجى إدخال سعر البيع (أكبر من صفر).';
      return;
    }

    this.submitting = true;
    this.sellError = '';

    this.deviceService.sell(this.sellTarget.id, {
      salePrice: price,
      soldStoreId: this.isAdmin ? this.sellForm.soldStoreId : undefined,
      buyerName: this.sellForm.buyerName.trim() || undefined,
      buyerPhone: this.sellForm.buyerPhone.trim() || undefined,
      notes: this.sellForm.notes.trim() || undefined
    }).subscribe({
      next: (sold) => {
        this.submitting = false;
        this.sellTarget = null;
        // فاتورة البيع Thermal فوراً بعد تأكيد البيع
        this.receipt.printDeviceSale(sold);
        this.toast.success(`تم تسجيل بيع الجهاز بسعر ${price} ج.م.`);
        this.load();
      },
      error: (err) => {
        this.submitting = false;
        this.sellError = err?.error?.message || 'تعذر تسجيل البيع.';
      }
    });
  }

  /** إعادة طباعة فاتورة جهاز مُباع (للأدمن، لأن الفروع لا ترى المباع) */
  printSale(d: DeviceResponse): void {
    this.receipt.printDeviceSale(d);
  }

  // ---------- Cancel sale / delete (admin) ----------
  askConfirm(kind: 'cancel-sale' | 'delete', device: DeviceResponse): void {
    this.confirmAction = { kind, device };
    this.confirmError = '';
  }

  closeConfirm(): void {
    if (this.submitting) return;
    this.confirmAction = null;
  }

  runConfirm(): void {
    if (this.submitting || !this.confirmAction) return;
    const { kind, device } = this.confirmAction;

    this.submitting = true;
    this.confirmError = '';

    const done = () => {
      this.submitting = false;
      this.confirmAction = null;
      this.toast.success(kind === 'delete' ? 'تم حذف الجهاز.' : 'تم إلغاء البيع وإرجاع الجهاز للمتاح.');
      this.load();
    };
    const fail = (err: any) => {
      this.submitting = false;
      this.confirmError = err?.error?.message || 'تعذر تنفيذ العملية.';
    };

    if (kind === 'delete') {
      this.deviceService.delete(device.id).subscribe({ next: done, error: fail });
    } else {
      this.deviceService.cancelSale(device.id).subscribe({ next: done, error: fail });
    }
  }

  trackDevice = (_: number, d: DeviceResponse) => d.id;

  private emptyForm() {
    return {
      storeId: null as number | null,
      brand: '',
      model: '',
      color: '',
      storage: '',
      imei: '',
      batteryHealth: null as number | null,
      hasBox: false,
      purchasePrice: null as number | null,
      purchaseDate: '',
      sellerName: '',
      notes: ''
    };
  }
}
