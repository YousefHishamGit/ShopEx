import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ConsignmentService } from '../../../services/consignment.service';
import { StockService } from '../../../services/stock.service';
import {
  Dealer,
  Warehouse,
  CreateConsignmentRequest,
  CreateConsignmentItemRequest,
  CreateDealerRequest
} from '../../../models/models';

@Component({
  selector: 'app-create-consignment',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-consignment.component.html'
})
export class CreateConsignmentComponent implements OnInit {
  dealers: Dealer[] = [];
  warehouses: Warehouse[] = [];

  selectedDealerId: number | null = null;
  selectedWarehouseId: number | null = null;
  agreedDays: number = 15;
  dueDate: string = '';
  notes: string = '';

  items: CreateConsignmentItemRequest[] = [
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
    private router: Router
  ) {}

  ngOnInit(): void {
    this.calculateDueDate();
    this.loadDealers();
    this.loadWarehouses();
  }

  calculateDueDate(): void {
    const d = new Date();
    d.setDate(d.getDate() + (Number(this.agreedDays) || 15));
    this.dueDate = d.toISOString().substring(0, 10);
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

  loadWarehouses(): void {
    this.stockService.getWarehouses(true).subscribe({
      next: (data) => {
        this.warehouses = data || [];
        if (this.warehouses.length > 0) {
          const defaultWh = this.warehouses.find((w) => w.isDefault) || this.warehouses[0];
          this.selectedWarehouseId = defaultWh.id;
        }
      },
      error: (err) => console.error('Error loading warehouses:', err)
    });
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
      alert('يرجى اختيار البنزينة / المحل.');
      return;
    }

    if (!this.items || this.items.length === 0) {
      alert('يرجى إضافة صنف واحد على الأقل.');
      return;
    }

    for (let idx = 0; idx < this.items.length; idx++) {
      const item = this.items[idx];
      if (!item.rmsItemID || item.rmsItemID <= 0) {
        alert(`السطر رقم (${idx + 1}): يرجى إدخال كود الصنف (RMS Item ID).`);
        return;
      }
      if (!item.quantityIssued || item.quantityIssued <= 0) {
        alert(`السطر رقم (${idx + 1}): كمية الصرف يجب أن تكون أكبر من صفر.`);
        return;
      }
      if (item.unitPrice < 0) {
        alert(`السطر رقم (${idx + 1}): سعر البيع لا يمكن أن يكون سالباً.`);
        return;
      }
      if (item.unitCost < 0) {
        alert(`السطر رقم (${idx + 1}): سعر التكلفة لا يمكن أن يكون سالباً.`);
        return;
      }
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
        this.router.navigate(['/consignments', res.id]);
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = err.error?.message || 'حدث خطأ أثناء حفظ إذن صرف البنزينة.';
      }
    });
  }

  // Quick Dealer Modal
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
