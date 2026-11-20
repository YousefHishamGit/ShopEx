import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { PurchaseLineDto, OpenWorksheetWithPurchasesRequest, Supplier, SupplierItem } from '../../models/models';

@Component({
  selector: 'app-open-worksheet',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './open-worksheet.component.html'
})
export class OpenWorksheetComponent implements OnInit {
  title: string = 'Worksheet - Purchase Order';
  notes: string = '';

  // Optional supplier filter to easily pick products
  suppliers: Supplier[] = [];
  selectedSupplierId: number | null = null;
  supplierItems: SupplierItem[] = [];
  loadingItems: boolean = false;

  items: PurchaseLineDto[] = [
    { itemId: 0, description: '', qty: 1, price: 0, storeId: 1, taxRate: 0 }
  ];

  submitting: boolean = false;
  isApproving: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadSuppliers();
  }

  loadSuppliers(): void {
    this.api.getAllSuppliers().subscribe({
      next: (data) => {
        this.suppliers = data || [];
      },
      error: (err) => console.error('Failed to load suppliers for worksheet items', err)
    });
  }

  onSupplierChange(): void {
    if (this.selectedSupplierId) {
      this.loadingItems = true;
      this.api.getSupplierItems(this.selectedSupplierId).subscribe({
        next: (items) => {
          this.supplierItems = items || [];
          this.loadingItems = false;
        },
        error: (err) => {
          console.error('Failed to load supplier items', err);
          this.loadingItems = false;
        }
      });
    } else {
      this.supplierItems = [];
    }
  }

  onItemSelect(index: number, itemId: number): void {
    const it = this.supplierItems.find(i => i.id === Number(itemId));
    if (it) {
      this.items[index].itemId = it.id;
      this.items[index].description = it.description;
      this.items[index].price = it.cost || it.price || 0;
      this.items[index].taxRate = it.taxRate || 0;
    }
  }

  addItemRow(): void {
    this.items.push({
      itemId: 0,
      description: '',
      qty: 1,
      price: 0,
      storeId: 1,
      taxRate: 0
    });
  }

  removeItemRow(index: number): void {
    if (this.items.length > 1) {
      this.items.splice(index, 1);
    }
  }

  get totalItemsCount(): number {
    return this.items.reduce((sum, i) => sum + (Number(i.qty) || 0), 0);
  }

  get totalAmount(): number {
    return this.items.reduce((sum, i) => {
      const line = (Number(i.qty) || 0) * (Number(i.price) || 0);
      const tax = line * ((Number(i.taxRate) || 0) / 100);
      return sum + line + tax;
    }, 0);
  }

  /**
   * Main flow: Create Worksheet with Purchases and immediately Approve it,
   * then reset back to Create Worksheet form.
   */
  approveAndSubmitWorksheet(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (this.items.length === 0 || this.items.some(i => !i.itemId || i.itemId <= 0 || i.qty <= 0)) {
      this.errorMessage = 'يرجى إدخال كود منتج صالح وتحديد كمية أكبر من صفر لجميع بنود الشراء.';
      return;
    }

    const req: OpenWorksheetWithPurchasesRequest = {
      title: this.title?.trim() || 'Worksheet Purchase Order',
      notes: this.notes?.trim() || undefined,
      items: this.items.map(i => ({
        itemId: Number(i.itemId),
        description: i.description?.trim(),
        qty: Number(i.qty),
        price: Number(i.price),
        storeId: Number(i.storeId || 1),
        taxRate: Number(i.taxRate || 0)
      }))
    };

    this.submitting = true;
    this.isApproving = true;

    // 1. Open Worksheet with Purchases
    this.api.openWorksheetWithPurchases(req).subscribe({
      next: (res) => {
        const createdId = res.worksheetId;

        // 2. Immediately Approve Worksheet
        this.api.approveWorksheet(createdId).subscribe({
          next: () => {
            this.submitting = false;
            this.isApproving = false;
            this.successMessage = `تم إنشاء واعتماد الـ Worksheet رقم #${createdId} بنجاح! تم تحويل الحالة إلى Approved (2).`;
            this.resetForm();
          },
          error: (err) => {
            this.submitting = false;
            this.isApproving = false;
            console.error('Error approving worksheet', err);
            this.errorMessage = `تم إنشاء الـ Worksheet #${createdId} ولكن تعذر اعتماده تلقائياً: ${err.error?.message || err.message}`;
          }
        });
      },
      error: (err) => {
        this.submitting = false;
        this.isApproving = false;
        console.error('Error opening worksheet with purchases', err);
        this.errorMessage = err.error?.message || err.message || 'فشل في إنشاء الـ Worksheet.';
      }
    });
  }

  /**
   * Optional Save as Draft without approval
   */
  saveDraft(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (this.items.length === 0 || this.items.some(i => !i.itemId || i.itemId <= 0 || i.qty <= 0)) {
      this.errorMessage = 'يرجى إدخال كود منتج صالح وتحديد كمية أكبر من صفر لجميع بنود الشراء.';
      return;
    }

    const req: OpenWorksheetWithPurchasesRequest = {
      title: this.title?.trim() || 'Worksheet Purchase Order',
      notes: this.notes?.trim() || undefined,
      items: this.items.map(i => ({
        itemId: Number(i.itemId),
        description: i.description?.trim(),
        qty: Number(i.qty),
        price: Number(i.price),
        storeId: Number(i.storeId || 1),
        taxRate: Number(i.taxRate || 0)
      }))
    };

    this.submitting = true;
    this.api.openWorksheetWithPurchases(req).subscribe({
      next: (res) => {
        this.submitting = false;
        this.successMessage = `تم حفظ الـ Worksheet #${res.worksheetId} كمسودة (PendingApproval - 1) بنجاح!`;
        this.resetForm();
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = err.error?.message || err.message || 'فشل في حفظ المسودة.';
      }
    });
  }

  resetForm(): void {
    this.title = 'Worksheet - Purchase Order';
    this.notes = '';
    this.items = [
      { itemId: 0, description: '', qty: 1, price: 0, storeId: 1, taxRate: 0 }
    ];
    this.selectedSupplierId = null;
    this.supplierItems = [];
    // Auto clear success message after 6 seconds
    setTimeout(() => {
      this.successMessage = '';
    }, 6000);
  }
}
