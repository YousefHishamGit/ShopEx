import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { StockService } from '../../services/stock.service';
import { ToastService } from '../../services/toast.service';
import { Warehouse, StockBalance, StockMovement, StockAdjustmentRequest } from '../../models/models';

const MOVEMENT_LABELS: Record<number, string> = {
  1: 'رصيد افتتاحي',
  2: 'شراء',
  3: 'مرتجع لمورد',
  4: 'بيع',
  5: 'مرتجع عميل',
  6: 'تسوية بالزيادة',
  7: 'تسوية بالخصم',
  8: 'تحويل وارد',
  9: 'تحويل صادر',
  10: 'صرف أمانة',
  11: 'مرتجع أمانة'
};

@Component({
  selector: 'app-stock',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './stock.component.html'
})
export class StockComponent implements OnInit, OnDestroy {
  warehouses: Warehouse[] = [];
  selectedWarehouseId: number | null = null;
  selectedWarehouse: Warehouse | null = null;

  balances: StockBalance[] = [];
  movements: StockMovement[] = [];

  activeTab: 'balances' | 'movements' = 'balances';
  searchQuery: string = '';

  // Pagination (server side)
  page = 1;
  readonly pageSize = 20;
  totalCount = 0;
  totalPages = 0;

  loading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  private search$ = new Subject<string>();
  private searchSub?: Subscription;

  // Adjustment Modal
  showAdjustModal: boolean = false;
  adjustRequest: StockAdjustmentRequest = {
    warehouseID: 0,
    rmsItemID: 0,
    quantity: 1,
    unitCost: 0,
    reason: '',
    createdBy: 'Admin'
  };
  adjusting: boolean = false;

  // New Warehouse Modal
  showWarehouseModal: boolean = false;
  newWarehouse = {
    name: '',
    location: '',
    isDefault: false,
    isActive: true
  };
  creatingWarehouse: boolean = false;

  constructor(
    private stockService: StockService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.searchSub = this.search$
      .pipe(debounceTime(350), distinctUntilChanged())
      .subscribe(() => {
        this.page = 1;
        this.loadBalances();
      });
    this.loadWarehouses();
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  loadWarehouses(): void {
    this.loading = true;
    this.stockService.getWarehouses(false).subscribe({
      next: (data) => {
        this.warehouses = data || [];
        if (this.warehouses.length > 0) {
          const defaultWh = this.warehouses.find(w => w.isDefault) || this.warehouses[0];
          this.onSelectWarehouse(defaultWh.id);
        } else {
          this.loading = false;
        }
      },
      error: (err) => {
        this.errorMessage = 'فشل في تحميل قائمة المخازن: ' + (err.error?.message || err.message);
        this.loading = false;
      }
    });
  }

  onSelectWarehouse(warehouseId: number): void {
    this.selectedWarehouseId = warehouseId;
    this.selectedWarehouse = this.warehouses.find(w => w.id === warehouseId) || null;
    this.page = 1;
    this.loadBalances();
    if (this.activeTab === 'movements') {
      this.loadMovements();
    } else {
      this.movements = [];
    }
  }

  loadBalances(): void {
    if (!this.selectedWarehouseId) return;
    this.loading = true;
    this.errorMessage = '';
    this.stockService.getBalances(this.selectedWarehouseId, this.page, this.pageSize, this.searchQuery.trim()).subscribe({
      next: (res) => {
        this.balances = res.items || [];
        this.totalCount = res.totalCount;
        this.totalPages = res.totalPages;
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'فشل في تحميل أرصدة المخزن: ' + (err.error?.message || err.message);
        this.loading = false;
      }
    });
  }

  loadMovements(): void {
    if (!this.selectedWarehouseId) return;
    this.loading = true;
    this.stockService.getMovements(this.selectedWarehouseId).subscribe({
      next: (data) => {
        this.movements = data || [];
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'فشل في تحميل حركات المخزن: ' + (err.error?.message || err.message);
        this.loading = false;
      }
    });
  }

  switchTab(tab: 'balances' | 'movements'): void {
    this.activeTab = tab;
    if (tab === 'movements') {
      this.loadMovements();
    }
  }

  onSearchChange(): void {
    this.search$.next(this.searchQuery.trim());
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages || p === this.page) return;
    this.page = p;
    this.loadBalances();
  }

  get rangeStart(): number {
    return this.totalCount === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }

  get rangeEnd(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  movementLabel(m: StockMovement): string {
    return MOVEMENT_LABELS[m.movementType] || m.movementTypeName;
  }

  // Open adjustment modal
  openAdjustModal(balance?: StockBalance): void {
    this.adjustRequest = {
      warehouseID: this.selectedWarehouseId || 1,
      rmsItemID: balance ? balance.rmsItemID : 0,
      quantity: 1,
      unitCost: balance ? balance.averageCost : 0,
      reason: 'تسوية جردية',
      createdBy: 'Admin'
    };
    this.errorMessage = '';
    this.successMessage = '';
    this.showAdjustModal = true;
  }

  closeAdjustModal(): void {
    this.showAdjustModal = false;
  }

  submitAdjustment(): void {
    if (this.adjustRequest.rmsItemID <= 0) {
      alert('يرجى إدخال رقم كود الصنف (RMS Item ID) بشكل صحيح.');
      return;
    }
    if (this.adjustRequest.quantity === 0) {
      alert('كمية التسوية لا يمكن أن تكون صفراً.');
      return;
    }

    this.adjusting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.stockService.adjustStock(this.adjustRequest).subscribe({
      next: () => {
        this.adjusting = false;
        this.showAdjustModal = false;
        this.successMessage = 'تمت التسوية المخزنية بنجاح!';
        this.toast.success(this.successMessage);
        this.loadBalances();
        if (this.activeTab === 'movements') {
          this.loadMovements();
        }
      },
      error: (err) => {
        this.adjusting = false;
        const msg = err.error?.message || err.message || 'حدث خطأ أثناء التسوية';
        this.toast.error(msg);
      }
    });
  }

  // Open Warehouse modal
  openWarehouseModal(): void {
    this.newWarehouse = {
      name: '',
      location: '',
      isDefault: false,
      isActive: true
    };
    this.showWarehouseModal = true;
  }

  closeWarehouseModal(): void {
    this.showWarehouseModal = false;
  }

  submitWarehouse(): void {
    if (!this.newWarehouse.name.trim()) {
      alert('اسم المخزن مطلوب.');
      return;
    }

    this.creatingWarehouse = true;
    this.stockService.createWarehouse(this.newWarehouse).subscribe({
      next: (created) => {
        this.creatingWarehouse = false;
        this.showWarehouseModal = false;
        this.warehouses.push(created);
        this.onSelectWarehouse(created.id);
        this.successMessage = `تم إنشاء المخزن '${created.name}' بنجاح!`;
      },
      error: (err) => {
        this.creatingWarehouse = false;
        alert('حدث خطأ أثناء إنشاء المخزن: ' + (err.error?.message || err.message));
      }
    });
  }
}
