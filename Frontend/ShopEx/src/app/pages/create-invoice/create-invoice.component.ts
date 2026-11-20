import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { StockService } from '../../services/stock.service';
import { Supplier, SupplierItem, CreateInvoiceRequest, CreateInvoiceItemRequest, Warehouse } from '../../models/models';

@Component({
  selector: 'app-create-invoice',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-invoice.component.html'
})
export class CreateInvoiceComponent implements OnInit {
  suppliers: Supplier[] = [];
  warehouses: Warehouse[] = [];
  selectedSupplierId: number | null = null;
  selectedWarehouseId: number | null = null;
  invoiceNumber: string = '';
  invoiceDate: string = new Date().toISOString().substring(0, 10);
  paymentType: number = 2; // 1 = Cash, 2 = Credit
  creditDays: number = 30;
  notes: string = '';
  initialPayment: number = 0;
  initialPaymentMethod: string = 'Cash';

  items: CreateInvoiceItemRequest[] = [

  ];

  submitting: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  constructor(
    private api: ApiService,
    private stockService: StockService,
    private router: Router
  ) {
    // Check if navigated with state from Supplier Items
    const navigation = this.router.getCurrentNavigation();
    if (navigation?.extras?.state) {
      const state = navigation.extras.state as { supplierId?: number; items?: SupplierItem[] };
      if (state.supplierId) {
        this.selectedSupplierId = state.supplierId;
      }
      if (state.items && state.items.length > 0) {
        this.items = state.items.map(i => ({
          itemId: i.id,
          description: i.description,
          quantity: 1,
          unitPrice: i.cost || i.price || 0,
          taxRate: i.taxRate || 0
        }));
      }
    }
  }

  ngOnInit(): void {
    this.stockService.getWarehouses(true).subscribe({
      next: (data) => {
        this.warehouses = data || [];
        if (this.warehouses.length > 0) {
          const defaultWh = this.warehouses.find(w => w.isDefault) || this.warehouses[0];
          this.selectedWarehouseId = defaultWh.id;
        }
      }
    });

    this.api.getAllSuppliers().subscribe({
      next: (data) => {
        this.suppliers = data || [];
        if (!this.selectedSupplierId && this.suppliers.length > 0) {
          this.selectedSupplierId = this.suppliers[0].id;
        }
      },
      error: (err) => console.error('Failed to load suppliers for dropdown', err)
    });
  }

  onSupplierChange(): void {
    if (this.selectedSupplierId) {
      this.api.getSupplierItems(this.selectedSupplierId).subscribe({
        next: (items) => {
          // If items table has single empty item, pre-fill suggestions if needed
        },
        error: (err) => console.error(err)
      });
    }
  }

  addItemRow(): void {
    this.items.push({
      itemId: 0,
      description: '',
      quantity: 1,
      unitPrice: 0,
      taxRate: 0
    });
  }

  removeItemRow(index: number): void {
    if (this.items.length > 1) {
      this.items.splice(index, 1);
    }
  }

  get totalAmount(): number {
    return this.items.reduce((sum, item) => {
      const line = (item.quantity || 0) * (item.unitPrice || 0);
      const tax = line * ((item.taxRate || 0) / 100);
      return sum + line + tax;
    }, 0);
  }

  generateInvoiceNumber(): void {
    const ts = Date.now().toString(36).toUpperCase();
    this.invoiceNumber = `INV-${ts}`;
  }

  submitInvoice(): void {
    this.errorMessage = '';
    this.successMessage = '';

    // Auto-generate invoice number if empty
    if (!this.invoiceNumber.trim()) {
      this.generateInvoiceNumber();
    }

    if (!this.selectedSupplierId) {
      this.errorMessage = 'Please select a supplier.';
      return;
    }
    if (this.paymentType === 2 && (this.creditDays === null || this.creditDays < 0)) {
      this.errorMessage = 'Credit days must be 0 or greater for credit invoices.';
      return;
    }
    if (this.items.length === 0 || this.items.some(i => !i.itemId || i.quantity <= 0)) {
      this.errorMessage = 'Each line item must have a valid Item ID and quantity > 0.';
      return;
    }

    const isCash = this.paymentType === 1;
    const initialPaymentAmount = isCash ? this.totalAmount : (this.initialPayment > 0 ? this.initialPayment : undefined);

    const payload: CreateInvoiceRequest = {
      supplierId: this.selectedSupplierId,
      warehouseID: this.selectedWarehouseId || undefined,
      invoiceNumber: this.invoiceNumber.trim(),
      invoiceDate: this.invoiceDate,
      paymentType: this.paymentType,
      creditDays: isCash ? undefined : (this.creditDays > 0 ? this.creditDays : undefined),
      notes: this.notes.trim(),
      initialPayment: initialPaymentAmount,
      initialPaymentMethod: isCash ? (this.initialPaymentMethod || 'Cash') : (this.initialPayment > 0 ? this.initialPaymentMethod : undefined),
      items: this.items.map(i => ({
        itemId: Number(i.itemId),
        description: i.description?.trim(),
        quantity: Number(i.quantity),
        unitPrice: Number(i.unitPrice),
        taxRate: Number(i.taxRate || 0)
      }))
    };

    this.submitting = true;
    this.api.createInvoice(payload).subscribe({
      next: (res) => {
        this.submitting = false;
        this.successMessage = `Invoice ${res.invoiceNumber} created successfully!`;
        setTimeout(() => {
          this.router.navigate(['/invoices', res.id], { queryParams: { print: 'true' } });
        }, 800);
      },
      error: (err) => {
        this.submitting = false;
        console.error('Error creating invoice', err);
        this.errorMessage = err.error?.message || err.message || 'Failed to create invoice.';
      }
    });
  }
}

