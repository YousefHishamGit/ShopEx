import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { BalanceService } from '../../services/balance.service';
import { ToastService } from '../../services/toast.service';
import {
  StoreBalanceGroup,
  BalanceTransactionItem,
  BalanceType,
  StoreOption,
  ChargeBalanceRequest,
  CloseStoreBalanceRequest
} from '../../models/models';

interface BalanceTypeMeta {
  id: BalanceType;
  name: string;
  /** Short English label used in compact badges */
  short: string;
  /** Tailwind classes for the badge */
  color: string;
  /** Tailwind class for the accent dot */
  dot: string;
}

@Component({
  selector: 'app-balances',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './balances.component.html'
})
export class BalancesComponent implements OnInit, OnDestroy {
  /** عدد الحركات التي يعرضها السجل (يطابق الحد في الـ API) */
  readonly ledgerLimit = 30;

  activeTab: 'summary' | 'ledger' = 'summary';

  // Raw data from the API
  groupedBalances: StoreBalanceGroup[] = [];
  ledgerItems: BalanceTransactionItem[] = [];
  stores: StoreOption[] = [];
  private storesMap = new Map<number, string>();

  // Derived data (computed once per load / filter change, not on every change-detection pass)
  filteredGroupedBalances: StoreBalanceGroup[] = [];
  filteredLedgerItems: BalanceTransactionItem[] = [];
  totalChargedAmount = 0;
  totalUsedAmount = 0;
  totalProfitAmount = 0;
  totalRemainingAmount = 0;
  activeWalletsCount = 0;

  loading = false;
  errorMessage = '';

  readonly balanceTypes: BalanceTypeMeta[] = [
    { id: BalanceType.VodafoneCash, name: 'Vodafone Cash (فودافون كاش)', short: 'Vodafone Cash', color: 'bg-red-50 text-red-700 border-red-200', dot: 'bg-red-500' },
    { id: BalanceType.OrangeCash, name: 'Orange Cash (أورنج كاش)', short: 'Orange Cash', color: 'bg-orange-50 text-orange-700 border-orange-200', dot: 'bg-orange-500' },
    { id: BalanceType.EtisalatCash, name: 'Etisalat Cash (اتصالات كاش)', short: 'Etisalat Cash', color: 'bg-emerald-50 text-emerald-700 border-emerald-200', dot: 'bg-emerald-500' },
    { id: BalanceType.instapay, name: 'Instapay (انستاباي)', short: 'Instapay', color: 'bg-purple-50 text-purple-700 border-purple-200', dot: 'bg-purple-500' },
    { id: BalanceType.Cards, name: 'Cards (فيزا / كروت)', short: 'Cards', color: 'bg-blue-50 text-blue-700 border-blue-200', dot: 'bg-blue-500' },
    { id: BalanceType.Fawry, name: 'Fawry (فوري)', short: 'Fawry', color: 'bg-amber-50 text-amber-700 border-amber-200', dot: 'bg-amber-500' }
  ];
  private readonly fallbackType = { name: '', short: '', color: 'bg-gray-100 text-gray-800 border-gray-200', dot: 'bg-gray-400' };

  readonly quickAmounts = [500, 1000, 1500, 2000, 5000];

  // Filters
  selectedStoreId: number | null = null;
  selectedBalanceType: number | null = null;
  searchQuery = '';
  ledgerTypeFilter: 'all' | 'charge' | 'closure' = 'all';

  // Charge Modal
  showChargeModal = false;
  submittingCharge = false;
  chargeError = '';
  chargeForm: {
    storeId: number | null;
    balanceType: number | null;
    amount: number | null;
    notes: string;
  } = { storeId: null, balanceType: BalanceType.VodafoneCash, amount: null, notes: '' };

  // Close Store Balance Modal
  showCloseModal = false;
  submittingClose = false;
  closeError = '';
  selectedGroupForClose: StoreBalanceGroup | null = null;
  closeForm: { amountUsed: number | null; profit: number | null; notes: string } = { amountUsed: null, profit: null, notes: '' };

  private loadSub?: Subscription;

  constructor(
    private balanceService: BalanceService,
    private toast: ToastService
  ) { }

  ngOnInit(): void {
    this.loadStores();
    this.loadData();
  }

  ngOnDestroy(): void {
    this.loadSub?.unsubscribe();
  }

  setTab(tab: 'summary' | 'ledger'): void {
    if (this.activeTab === tab) return;
    this.activeTab = tab;
    this.loadData();
  }

  loadStores(): void {
    this.balanceService.getStores().subscribe({
      next: (data) => {
        this.stores = data || [];
        this.storesMap.clear();
        for (const s of this.stores) {
          // API returns { id, name }
          this.storesMap.set(s.id, s.name);
        }
        // Store names are needed for search / display
        this.applyFilters();
      },
      error: (err) => console.error('Error fetching stores:', err)
    });
  }

  loadData(): void {
    // Cancel any in-flight request so a slow older response can't overwrite a newer one
    this.loadSub?.unsubscribe();
    this.loading = true;
    this.errorMessage = '';

    const storeId = this.selectedStoreId || undefined;
    const type = this.selectedBalanceType || undefined;

    if (this.activeTab === 'summary') {
      this.loadSub = this.balanceService.getGrouped(storeId, type).subscribe({
        next: (data) => {
          this.groupedBalances = data || [];
          this.computeTotals();
          this.applyFilters();
          this.loading = false;
        },
        error: (err) => {
          console.error('Error loading grouped balances:', err);
          this.errorMessage = err.error?.message || 'تعذر تحميل الأرصدة المجمعة.';
          this.loading = false;
        }
      });
    } else {
      this.loadSub = this.balanceService.getLedger(storeId, type).subscribe({
        next: (data) => {
          this.ledgerItems = data || [];
          this.applyFilters();
          this.loading = false;
        },
        error: (err) => {
          console.error('Error loading ledger:', err);
          this.errorMessage = err.error?.message || 'تعذر تحميل كشف الحركات.';
          this.loading = false;
        }
      });
    }
  }

  // ----------------------------------------------------------------
  // Derived data
  // ----------------------------------------------------------------
  applyFilters(): void {
    const q = this.searchQuery.trim().toLowerCase();

    this.filteredGroupedBalances = !q
      ? this.groupedBalances
      : this.groupedBalances.filter((b) =>
        this.getStoreName(b.storeId).toLowerCase().includes(q) ||
        (b.balanceTypeName || '').toLowerCase().includes(q) ||
        b.storeId.toString().includes(q));

    this.filteredLedgerItems = this.ledgerItems.filter((t) => {
      if (this.ledgerTypeFilter === 'charge' && t.transactionType !== 'Charge') return false;
      if (this.ledgerTypeFilter === 'closure' && t.transactionType !== 'Closure') return false;
      if (!q) return true;
      return (
        this.getStoreName(t.storeId).toLowerCase().includes(q) ||
        (t.balanceTypeName || '').toLowerCase().includes(q) ||
        (t.notes || '').toLowerCase().includes(q) ||
        t.storeId.toString().includes(q)
      );
    });
  }

  private computeTotals(): void {
    let charged = 0, used = 0, profit = 0, remaining = 0, active = 0;
    for (const b of this.groupedBalances) {
      charged += b.totalCharged || 0;
      used += b.totalUsed || 0;
      profit += b.totalProfit || 0;
      remaining += b.currentBalance || 0;
      if (b.currentBalance > 0) active++;
    }
    this.totalChargedAmount = charged;
    this.totalUsedAmount = used;
    this.totalProfitAmount = profit;
    this.totalRemainingAmount = remaining;
    this.activeWalletsCount = active;
  }

  usedPercent(g: StoreBalanceGroup): number {
    if (!g.totalCharged) return 0;
    return Math.min(100, Math.max(0, (g.totalUsed / g.totalCharged) * 100));
  }

  getStoreName(storeId: number): string {
    return this.storesMap.get(storeId) || `فرع #${storeId}`;
  }

  getBalanceTypeBadge(type: BalanceType): { name: string; short: string; color: string; dot: string } {
    const found = this.balanceTypes.find((t) => t.id === type);
    return found ?? { ...this.fallbackType, name: `وسيلة ${type}`, short: `وسيلة ${type}` };
  }

  trackGroup = (_: number, g: StoreBalanceGroup) => `${g.storeId}-${g.balanceType}`;
  trackTx = (_: number, t: BalanceTransactionItem) => `${t.transactionType}-${t.id}`;

  // ----------------------------------------------------------------
  // Modal 1: Charge Balance
  // ----------------------------------------------------------------
  openChargeModal(preselected?: StoreBalanceGroup): void {
    this.chargeForm = {
      storeId: preselected ? preselected.storeId : (this.selectedStoreId ?? (this.stores.length > 0 ? this.stores[0].id : null)),
      balanceType: preselected ? preselected.balanceType : (this.selectedBalanceType ?? BalanceType.VodafoneCash),
      amount: null,
      notes: ''
    };
    this.chargeError = '';
    this.showChargeModal = true;
  }

  closeChargeModal(): void {
    if (this.submittingCharge) return;
    this.showChargeModal = false;
  }

  /** الرصيد الحالي للفرع/الوسيلة المختارين في نموذج الشحن (للمعاينة) */
  get chargeFormCurrentBalance(): number {
    const g = this.groupedBalances.find(
      (x) => x.storeId === Number(this.chargeForm.storeId) && x.balanceType === Number(this.chargeForm.balanceType)
    );
    return g ? g.currentBalance : 0;
  }

  submitCharge(): void {
    if (this.submittingCharge) return;

    if (!this.chargeForm.storeId) {
      this.chargeError = 'يرجى اختيار الفرع.';
      return;
    }
    if (!this.chargeForm.balanceType) {
      this.chargeError = 'يرجى اختيار نوع وسيلة الدفع.';
      return;
    }
    if (!this.chargeForm.amount || this.chargeForm.amount <= 0) {
      this.chargeError = 'يرجى إدخال مبلغ شحن أكبر من الصفر.';
      return;
    }

    this.submittingCharge = true;
    this.chargeError = '';

    const req: ChargeBalanceRequest = {
      storeId: Number(this.chargeForm.storeId),
      balanceType: Number(this.chargeForm.balanceType),
      amount: Number(this.chargeForm.amount),
      notes: this.chargeForm.notes?.trim() || undefined
    };

    this.balanceService.charge(req).subscribe({
      next: () => {
        this.submittingCharge = false;
        this.showChargeModal = false;
        this.toast.success(`تم شحن مبلغ ${req.amount} ج.م للفرع بنجاح!`);
        this.loadData();
      },
      error: (err) => {
        this.submittingCharge = false;
        this.chargeError = err.error?.message || 'حدث خطأ أثناء شحن الرصيد.';
        this.toast.error(this.chargeError);
      }
    });
  }

  // ----------------------------------------------------------------
  // Modal 2: Close Store Balance
  // ----------------------------------------------------------------
  openCloseModal(group: StoreBalanceGroup, event?: MouseEvent): void {
    if (event) event.stopPropagation();
    this.selectedGroupForClose = group;
    this.closeForm = { amountUsed: null, profit: null, notes: '' };
    this.closeError = '';
    this.showCloseModal = true;
  }

  closeCloseModal(): void {
    if (this.submittingClose) return;
    this.showCloseModal = false;
    this.selectedGroupForClose = null;
  }

  /** الرصيد المتبقي بعد التقفيل (للمعاينة داخل النموذج) */
  get remainingAfterClose(): number {
    const cur = this.selectedGroupForClose?.currentBalance ?? 0;
    return cur - (Number(this.closeForm.amountUsed) || 0);
  }

  submitClose(): void {
    if (!this.selectedGroupForClose || this.submittingClose) return;

    const amount = Number(this.closeForm.amountUsed);
    if (!amount || amount <= 0) {
      this.closeError = 'يرجى إدخال مبلغ الاستهلاك أكبر من الصفر.';
      return;
    }

    if (amount > this.selectedGroupForClose.currentBalance) {
      this.closeError = `المبلغ المراد تقفيله (${amount}) يتجاوز الرصيد المتاح للفرع حالياً (${this.selectedGroupForClose.currentBalance}).`;
      return;
    }

    const profitRaw = this.closeForm.profit as unknown;
    if (profitRaw === null || profitRaw === undefined || profitRaw === '') {
      this.closeError = 'يرجى إدخال الربح (اكتب 0 لو مفيش ربح).';
      return;
    }
    const profit = Number(profitRaw);
    if (isNaN(profit) || profit < 0) {
      this.closeError = 'الربح لا يمكن أن يكون سالباً.';
      return;
    }

    this.submittingClose = true;
    this.closeError = '';

    const req: CloseStoreBalanceRequest = {
      storeId: this.selectedGroupForClose.storeId,
      balanceType: this.selectedGroupForClose.balanceType,
      amountUsed: amount,
      profit,
      notes: this.closeForm.notes?.trim() || undefined
    };

    this.balanceService.closeStore(req).subscribe({
      next: () => {
        this.submittingClose = false;
        this.showCloseModal = false;
        this.selectedGroupForClose = null;
        this.toast.success(`تم تقفيل استهلاك قدره ${req.amountUsed} ج.م بنجاح!`);
        this.loadData();
      },
      error: (err) => {
        this.submittingClose = false;
        this.closeError = err.error?.message || 'حدث خطأ أثناء تسجيل التقفيل.';
        this.toast.error(this.closeError);
      }
    });
  }

  viewGroupLedger(group: StoreBalanceGroup): void {
    this.selectedStoreId = group.storeId;
    this.selectedBalanceType = group.balanceType;
    if (this.activeTab === 'ledger') {
      this.loadData();
    } else {
      this.setTab('ledger');
    }
  }

  clearFilters(): void {
    this.selectedStoreId = null;
    this.selectedBalanceType = null;
    this.searchQuery = '';
    this.ledgerTypeFilter = 'all';
    this.loadData();
  }

  get hasActiveFilters(): boolean {
    return !!(this.selectedStoreId || this.selectedBalanceType || this.searchQuery.trim() || this.ledgerTypeFilter !== 'all');
  }
}
