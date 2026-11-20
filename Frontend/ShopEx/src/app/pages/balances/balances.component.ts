import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BalanceService } from '../../services/balance.service';
import {
  StoreBalanceGroup,
  BalanceTransactionItem,
  BalanceType,
  StoreOption,
  ChargeBalanceRequest,
  CloseStoreBalanceRequest
} from '../../models/models';

@Component({
  selector: 'app-balances',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './balances.component.html'
})
export class BalancesComponent implements OnInit {
  // Active Tab: 'summary' (الأرصدة المجمعة لكل فرع) أو 'ledger' (كشف التحويلات والحركات التفصيلية)
  activeTab: 'summary' | 'ledger' = 'summary';

  // Data
  groupedBalances: StoreBalanceGroup[] = [];
  ledgerItems: BalanceTransactionItem[] = [];
  stores: StoreOption[] = [];
  storesMap: Map<number, string> = new Map();

  loading = false;
  errorMessage = '';

  // Balance Types
  readonly balanceTypes = [
    { id: BalanceType.VodafoneCash, name: 'Vodafone Cash (فودافون كاش)', color: 'bg-red-50 text-red-700 border-red-200' },
    { id: BalanceType.OrangeCash, name: 'Orange Cash (أورنج كاش)', color: 'bg-orange-50 text-orange-700 border-orange-200' },
    { id: BalanceType.EtisalatCash, name: 'Etisalat Cash (اتصالات كاش)', color: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
    { id: BalanceType.instapay, name: 'Instapay (انستاباي)', color: 'bg-purple-50 text-purple-700 border-purple-200' },
    { id: BalanceType.Cards, name: 'Cards (فيزا / كروت)', color: 'bg-blue-50 text-blue-700 border-blue-200' },
    { id: BalanceType.Fawry, name: 'Fawry (فوري)', color: 'bg-amber-50 text-amber-700 border-amber-200' }
  ];

  // Filters
  selectedStoreId: number | null = null;
  selectedBalanceType: number | null = null;
  searchQuery: string = '';
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
  } = {
      storeId: null,
      balanceType: BalanceType.VodafoneCash,
      amount: null,
      notes: ''
    };

  // Close Store Balance Modal (تقفيل الرصيد على مستوى الفرع والوسيلة)
  showCloseModal = false;
  submittingClose = false;
  closeError = '';
  selectedGroupForClose: StoreBalanceGroup | null = null;
  closeForm: {
    amountUsed: number | null;
    notes: string;
  } = {
      amountUsed: null,
      notes: ''
    };

  constructor(private balanceService: BalanceService) { }

  ngOnInit(): void {
    this.loadStores();
    this.loadData();
  }

  setTab(tab: 'summary' | 'ledger'): void {
    this.activeTab = tab;
    this.loadData();
  }

  loadStores(): void {
    this.balanceService.getStores().subscribe({
      next: (data) => {
        this.stores = data || [];
        this.storesMap.clear();
        for (const s of this.stores) {
          this.storesMap.set(s.id, s.name);
        }
      },
      error: (err) => console.error('Error fetching stores:', err)
    });
  }

  loadData(): void {
    this.loading = true;
    this.errorMessage = '';

    if (this.activeTab === 'summary') {
      this.balanceService
        .getGrouped(this.selectedStoreId || undefined, this.selectedBalanceType || undefined)
        .subscribe({
          next: (data) => {
            this.groupedBalances = data || [];
            this.loading = false;
          },
          error: (err) => {
            console.error('Error loading grouped balances:', err);
            this.errorMessage = err.error?.message || 'تعذر تحميل الأرصدة المجمعة.';
            this.loading = false;
          }
        });
    } else {
      this.balanceService
        .getLedger(this.selectedStoreId || undefined, this.selectedBalanceType || undefined)
        .subscribe({
          next: (data) => {
            this.ledgerItems = data || [];
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
  // Filtered Lists & Computed KPIs
  // ----------------------------------------------------------------
  get filteredGroupedBalances(): StoreBalanceGroup[] {
    let list = this.groupedBalances;

    if (this.searchQuery && this.searchQuery.trim()) {
      const q = this.searchQuery.trim().toLowerCase();
      list = list.filter((b) => {
        const storeName = this.getStoreName(b.storeId).toLowerCase();
        const typeName = (b.balanceTypeName || '').toLowerCase();
        return storeName.includes(q) || typeName.includes(q) || b.storeId.toString().includes(q);
      });
    }

    return list;
  }

  get filteredLedgerItems(): BalanceTransactionItem[] {
    let list = this.ledgerItems;

    if (this.ledgerTypeFilter === 'charge') {
      list = list.filter((t) => t.transactionType.includes('شحن'));
    } else if (this.ledgerTypeFilter === 'closure') {
      list = list.filter((t) => t.transactionType.includes('تقفيل'));
    }

    if (this.searchQuery && this.searchQuery.trim()) {
      const q = this.searchQuery.trim().toLowerCase();
      list = list.filter((t) => {
        const storeName = this.getStoreName(t.storeId).toLowerCase();
        const typeName = (t.balanceTypeName || '').toLowerCase();
        const notes = (t.notes || '').toLowerCase();
        return (
          storeName.includes(q) ||
          typeName.includes(q) ||
          notes.includes(q) ||
          t.id.toString().includes(q) ||
          t.storeId.toString().includes(q)
        );
      });
    }

    return list;
  }

  get totalChargedAmount(): number {
    return this.groupedBalances.reduce((sum, b) => sum + (b.totalCharged || 0), 0);
  }

  get totalUsedAmount(): number {
    return this.groupedBalances.reduce((sum, b) => sum + (b.totalUsed || 0), 0);
  }

  get totalRemainingAmount(): number {
    return this.groupedBalances.reduce((sum, b) => sum + (b.currentBalance || 0), 0);
  }

  get activeWalletsCount(): number {
    return this.groupedBalances.filter((b) => b.currentBalance > 0).length;
  }

  getStoreName(storeId: number): string {
    return this.storesMap.get(storeId) || `فرع #${storeId}`;
  }

  getBalanceTypeBadge(type: BalanceType): { name: string; color: string } {
    const found = this.balanceTypes.find((t) => t.id === type);
    return found
      ? { name: found.name, color: found.color }
      : { name: `وسيلة ${type}`, color: 'bg-gray-100 text-gray-800 border-gray-200' };
  }

  // ----------------------------------------------------------------
  // Modal 1: Charge Balance
  // ----------------------------------------------------------------
  openChargeModal(preselected?: StoreBalanceGroup): void {
    this.chargeForm = {
      storeId: preselected ? preselected.storeId : (this.stores.length > 0 ? this.stores[0].id : null),
      balanceType: preselected ? preselected.balanceType : BalanceType.VodafoneCash,
      amount: null,
      notes: ''
    };
    this.chargeError = '';
    this.showChargeModal = true;
  }

  closeChargeModal(): void {
    this.showChargeModal = false;
  }

  submitCharge(): void {
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
      notes: this.chargeForm.notes
    };

    this.balanceService.charge(req).subscribe({
      next: () => {
        this.submittingCharge = false;
        this.closeChargeModal();
        this.loadData();
      },
      error: (err) => {
        this.submittingCharge = false;
        this.chargeError = err.error?.message || 'حدث خطأ أثناء شحن الرصيد.';
      }
    });
  }

  // ----------------------------------------------------------------
  // Modal 2: Close Store Balance (تقفيل رصيد الفرع التراكمي)
  // ----------------------------------------------------------------
  openCloseModal(group: StoreBalanceGroup, event?: MouseEvent): void {
    if (event) event.stopPropagation();
    this.selectedGroupForClose = group;
    this.closeForm = {
      amountUsed: null,
      notes: ''
    };
    this.closeError = '';
    this.showCloseModal = true;
  }

  closeCloseModal(): void {
    this.showCloseModal = false;
    this.selectedGroupForClose = null;
  }

  submitClose(): void {
    if (!this.selectedGroupForClose) return;

    if (!this.closeForm.amountUsed || this.closeForm.amountUsed <= 0) {
      this.closeError = 'يرجى إدخال مبلغ الاستهلاك أكبر من الصفر.';
      return;
    }

    if (this.closeForm.amountUsed > this.selectedGroupForClose.currentBalance) {
      this.closeError = `المبلغ المراد تقفيله (${this.closeForm.amountUsed}) يتجاوز الرصيد المتاح للفرع حالياً (${this.selectedGroupForClose.currentBalance}).`;
      return;
    }

    this.submittingClose = true;
    this.closeError = '';

    const req: CloseStoreBalanceRequest = {
      storeId: this.selectedGroupForClose.storeId,
      balanceType: this.selectedGroupForClose.balanceType,
      amountUsed: Number(this.closeForm.amountUsed),
      notes: this.closeForm.notes
    };

    this.balanceService.closeStore(req).subscribe({
      next: () => {
        this.submittingClose = false;
        this.closeCloseModal();
        this.loadData();
      },
      error: (err) => {
        this.submittingClose = false;
        this.closeError = err.error?.message || 'حدث خطأ أثناء تسجيل التقفيل.';
      }
    });
  }

  viewGroupLedger(group: StoreBalanceGroup): void {
    this.selectedStoreId = group.storeId;
    this.selectedBalanceType = group.balanceType;
    this.setTab('ledger');
  }
}
