import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StockService } from '../../services/stock.service';
import { Warehouse, StockBalance, StockMovement, StockAdjustmentRequest } from '../../models/models';

@Component({
  selector: 'app-stock',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './stock.component.html'
})
export class StockComponent implements OnInit {
  warehouses: Warehouse[] = [];
  selectedWarehouseId: number | null = null;
  selectedWarehouse: Warehouse | null = null;

  balances: StockBalance[] = [];
  filteredBalances: StockBalance[] = [];
  movements: StockMovement[] = [];

  activeTab: 'balances' | 'movements' = 'balances';
  lowStockOnly: boolean = false;
  searchQuery: string = '';

  loading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // Stats
  totalItemsCount: number = 0;
  totalStockValue: number = 0;
  lowStockCount: number = 0;

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

  constructor(private stockService: StockService) {}

  ngOnInit(): void {
    this.loadWarehouses();
  }

  loadWarehouses(): void {
    this.loading = true;
    this.stockService.getWarehouses(false).subscribe({
      next: (data) => {
        this.warehouses = data || [];
        if (this.warehouses.length > 0) {
          // Select default or first
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
    this.loadBalances();
    if (this.activeTab === 'movements') {
      this.loadMovements();
    }
  }

  loadBalances(): void {
    if (!this.selectedWarehouseId) return;
    this.loading = true;
    this.errorMessage = '';
    this.stockService.getBalances(this.selectedWarehouseId, this.lowStockOnly).subscribe({
      next: (data) => {
        this.balances = data || [];
        this.calculateStats();
        this.applyFilter();
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
    if (tab === 'movements' && this.movements.length === 0) {
      this.loadMovements();
    }
  }

  onToggleLowStock(): void {
    this.lowStockOnly = !this.lowStockOnly;
    this.loadBalances();
  }

  onSearch(): void {
    this.applyFilter();
  }

  applyFilter(): void {
    if (!this.searchQuery.trim()) {
      this.filteredBalances = [...this.balances];
    } else {
      const q = this.searchQuery.trim().toLowerCase();
      this.filteredBalances = this.balances.filter(b =>
        b.rmsItemID.toString().includes(q)
      );
    }
  }

  calculateStats(): void {
    this.totalItemsCount = this.balances.length;
    this.totalStockValue = this.balances.reduce((acc, b) => acc + (b.totalValue || 0), 0);
    this.lowStockCount = this.balances.filter(b => b.isLowStock).length;
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
        this.loadBalances();
        if (this.activeTab === 'movements') {
          this.loadMovements();
        }
      },
      error: (err) => {
        this.adjusting = false;
        alert('حدث خطأ أثناء التسوية: ' + (err.error?.message || err.message));
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
