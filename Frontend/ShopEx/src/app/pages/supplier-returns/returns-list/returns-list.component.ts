import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SupplierReturnService } from '../../../services/supplier-return.service';
import { StockService } from '../../../services/stock.service';
import { SupplierReturnSummary, Warehouse, SupplierReturnStatus, SupplierReturnSettlementType } from '../../../models/models';

@Component({
  selector: 'app-returns-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './returns-list.component.html'
})
export class ReturnsListComponent implements OnInit {
  returns: SupplierReturnSummary[] = [];
  warehouses: Warehouse[] = [];

  selectedStatus: string = 'ALL';
  selectedWarehouseId: number | null = null;
  searchQuery: string = '';

  loading: boolean = false;
  errorMessage: string = '';

  // Stats
  countTotal: number = 0;
  countDraft: number = 0;
  countConfirmed: number = 0;
  totalPendingRefund: number = 0;

  constructor(
    private returnService: SupplierReturnService,
    private stockService: StockService
  ) {}

  ngOnInit(): void {
    this.loadWarehouses();
    this.loadReturns();
  }

  loadWarehouses(): void {
    this.stockService.getWarehouses(true).subscribe({
      next: (data) => this.warehouses = data || []
    });
  }

  loadReturns(): void {
    this.loading = true;
    this.errorMessage = '';

    const statusParam = this.selectedStatus === 'ALL' ? undefined : parseInt(this.selectedStatus, 10);
    const warehouseParam = this.selectedWarehouseId ? this.selectedWarehouseId : undefined;

    this.returnService.getReturns(undefined, statusParam, warehouseParam).subscribe({
      next: (data) => {
        this.returns = data || [];
        this.calculateStats();
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'فشل في تحميل مرتجعات الموردين: ' + (err.error?.message || err.message);
        this.loading = false;
      }
    });
  }

  calculateStats(): void {
    this.countTotal = this.returns.length;
    this.countDraft = this.returns.filter(r => r.status === SupplierReturnStatus.Draft).length;
    this.countConfirmed = this.returns.filter(r => r.status === SupplierReturnStatus.Confirmed).length;
    this.totalPendingRefund = this.returns
      .filter(r => r.status === SupplierReturnStatus.Confirmed && r.settlementType === SupplierReturnSettlementType.CashRefund)
      .reduce((sum, r) => sum + (r.pendingRefund || 0), 0);
  }

  onFilterStatus(status: string): void {
    this.selectedStatus = status;
    this.loadReturns();
  }

  onFilterWarehouse(): void {
    this.loadReturns();
  }

  get filteredReturns(): SupplierReturnSummary[] {
    if (!this.searchQuery.trim()) return this.returns;
    const q = this.searchQuery.trim().toLowerCase();
    return this.returns.filter(r =>
      r.returnNumber.toLowerCase().includes(q) ||
      r.rmsSupplierID.toString().includes(q) ||
      r.warehouseName.toLowerCase().includes(q)
    );
  }
}
