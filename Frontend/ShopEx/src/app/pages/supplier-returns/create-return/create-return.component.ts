import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../../services/api.service';
import { StockService } from '../../../services/stock.service';
import { SupplierReturnService } from '../../../services/supplier-return.service';
import {
  Supplier,
  Warehouse,
  CreateSupplierReturnRequest,
  CreateSupplierReturnItemRequest,
  SupplierReturnSettlementType
} from '../../../models/models';

@Component({
  selector: 'app-create-return',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-return.component.html'
})
export class CreateReturnComponent implements OnInit {
  suppliers: Supplier[] = [];
  warehouses: Warehouse[] = [];

  selectedSupplierId: number | null = null;
  selectedWarehouseId: number | null = null;
  supplierInvoiceId: number | null = null;
  returnDate: string = new Date().toISOString().substring(0, 10);
  settlementType: SupplierReturnSettlementType = SupplierReturnSettlementType.CreditToAccount;
  notes: string = '';

  items: CreateSupplierReturnItemRequest[] = [
    {
      rmsItemID: 0,
      quantity: 1,
      unitPrice: 0,
      reason: ''
    }
  ];

  submitting: boolean = false;
  errorMessage: string = '';

  constructor(
    private api: ApiService,
    private stockService: StockService,
    private returnService: SupplierReturnService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    // Check query params for preselected supplier or invoice
    this.route.queryParams.subscribe(params => {
      if (params['supplierId']) {
        this.selectedSupplierId = Number(params['supplierId']);
      }
      if (params['supplierInvoiceId']) {
        this.supplierInvoiceId = Number(params['supplierInvoiceId']);
      }
    });

    // Load suppliers
    this.api.getAllSuppliers().subscribe({
      next: (data) => {
        this.suppliers = data || [];
        if (this.suppliers.length > 0 && !this.selectedSupplierId) {
          this.selectedSupplierId = this.suppliers[0].id;
        }
      }
    });

    // Load warehouses
    this.stockService.getWarehouses(true).subscribe({
      next: (data) => {
        this.warehouses = data || [];
        if (this.warehouses.length > 0) {
          const defaultWh = this.warehouses.find(w => w.isDefault) || this.warehouses[0];
          this.selectedWarehouseId = defaultWh.id;
        }
      }
    });
  }

  addItem(): void {
    this.items.push({
      rmsItemID: 0,
      quantity: 1,
      unitPrice: 0,
      reason: ''
    });
  }

  removeItem(index: number): void {
    if (this.items.length > 1) {
      this.items.splice(index, 1);
    }
  }

  get totalAmount(): number {
    return this.items.reduce((sum, i) => sum + ((i.quantity || 0) * (i.unitPrice || 0)), 0);
  }

  submitReturn(): void {
    if (!this.selectedSupplierId) {
      alert('يرجى اختيار المورد.');
      return;
    }

    if (!this.selectedWarehouseId) {
      alert('يرجى اختيار المخزن.');
      return;
    }

    for (const item of this.items) {
      if (!item.rmsItemID || item.rmsItemID <= 0) {
        alert('يرجى إدخال كود الصنف لكل سطر مرتجع.');
        return;
      }
      if (!item.quantity || item.quantity <= 0) {
        alert('الكمية يجب أن تكون أكبر من صفر.');
        return;
      }
      if (item.unitPrice < 0) {
        alert('سعر الوحدة لا يمكن أن يكون سالباً.');
        return;
      }
    }

    const request: CreateSupplierReturnRequest = {
      rmsSupplierID: this.selectedSupplierId,
      warehouseID: this.selectedWarehouseId,
      supplierInvoiceID: this.supplierInvoiceId || undefined,
      returnDate: this.returnDate,
      settlementType: Number(this.settlementType),
      notes: this.notes.trim() || undefined,
      items: this.items.map(i => ({
        rmsItemID: Number(i.rmsItemID),
        quantity: Number(i.quantity),
        unitPrice: Number(i.unitPrice),
        reason: i.reason?.trim() || undefined
      }))
    };

    this.submitting = true;
    this.errorMessage = '';

    this.returnService.createReturn(request).subscribe({
      next: (created) => {
        this.submitting = false;
        this.router.navigate(['/supplier-returns', created.id]);
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = 'فشل في حفظ المرتجع: ' + (err.error?.message || err.message);
      }
    });
  }
}
