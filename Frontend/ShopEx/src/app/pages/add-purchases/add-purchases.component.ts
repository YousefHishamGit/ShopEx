import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { PurchaseLineDto, Supplier, SupplierItem, StoreOption } from '../../models/models';
import { TabbedSelectComponent, TabbedSelectItem } from '../../components/tabbed-select/tabbed-select.component';

export interface ExtendedPurchaseLine extends PurchaseLineDto {
  dbCost?: number;
  dbPrice?: number;
  isLoadingDb?: boolean;
}

@Component({
  selector: 'app-add-purchases',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TabbedSelectComponent],
  templateUrl: './add-purchases.component.html'
})
export class AddPurchasesComponent implements OnInit {
  worksheetId!: number;
  stores: StoreOption[] = [];
  defaultStoreId: number = 1;

  items: ExtendedPurchaseLine[] = [
    { itemId: 0, description: '', qty: 1, price: 0, storeId: 1, taxRate: 0, dbCost: 0, dbPrice: 0 }
  ];

  suppliers: Supplier[] = [];
  selectedSupplierId: number | null = null;
  supplierItems: SupplierItem[] = [];
  generalItems: SupplierItem[] = [];
  loadingItems: boolean = false;

  submitting: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  constructor(
    private api: ApiService,
    private toast: ToastService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      if (idParam) {
        this.worksheetId = parseInt(idParam, 10);
      }
    });

    this.loadStores();
    this.loadSuppliers();
    this.loadGeneralItems();
  }

  loadStores(): void {
    this.api.getStores().subscribe({
      next: (data) => {
        this.stores = data || [];
        if (this.stores.length > 0) {
          this.defaultStoreId = this.stores[0].storeId;
          if (this.items.length > 0 && (!this.items[0].storeId || this.items[0].storeId === 1)) {
            this.items[0].storeId = this.defaultStoreId;
          }
        }
      },
      error: (err) => console.error('Failed to load stores', err)
    });
  }

  loadSuppliers(): void {
    this.api.getAllSuppliers().subscribe({
      next: (data) => {
        this.suppliers = data || [];
      }
    });
  }

  loadGeneralItems(): void {
    this.api.searchItems('', 100).subscribe({
      next: (data) => {
        this.generalItems = data || [];
      }
    });
  }

  applyStoreToAll(storeId: number): void {
    if (!storeId) return;
    this.defaultStoreId = storeId;
    this.items.forEach(i => i.storeId = Number(storeId));
    const storeObj = this.stores.find(s => s.storeId === Number(storeId));
    this.toast.info(`تم تطبيق (${storeObj ? storeObj.storeName : 'الفرع'}) على جميع البنود.`);
  }

  get selectableSuppliers(): TabbedSelectItem[] {
    return this.suppliers.map(s => ({
      id: s.id,
      title: s.supplierName,
      subtitle: s.code ? `كود: ${s.code}` : `مورد #${s.id}`,
      icon: 'storefront',
      raw: s
    }));
  }

  onSupplierSelect(item: TabbedSelectItem): void {
    this.selectedSupplierId = Number(item.id);
    this.loadingItems = true;
    this.api.getSupplierItems(this.selectedSupplierId).subscribe({
      next: (items) => {
        this.supplierItems = items || [];
        this.loadingItems = false;
      },
      error: () => {
        this.loadingItems = false;
        this.toast.error('فشل في جلب أصناف المورد');
      }
    });
  }

  onSupplierClear(): void {
    this.selectedSupplierId = null;
    this.supplierItems = [];
  }

  get activeItemsList(): SupplierItem[] {
    return this.supplierItems.length > 0 ? this.supplierItems : this.generalItems;
  }

  get selectableItems(): TabbedSelectItem[] {
    return this.activeItemsList.map(item => ({
      id: item.id,
      title: item.description,
      subtitle: item.itemLookupCode ? `باركود: ${item.itemLookupCode} | تكلفة: ${item.cost || 0} ج.م` : `صنف #${item.id} | تكلفة: ${item.cost || 0} ج.م`,
      price: (item.cost && item.cost > 0) ? item.cost : (item.price || 0),
      cost: item.cost,
      quantity: item.quantity,
      badge: (item.cost && item.cost > 0) ? `تكلفة: ${item.cost} ج.م` : (item.price ? `بيع: ${item.price} ج.م` : undefined),
      icon: 'inventory_2',
      raw: item
    }));
  }

  onItemSelect(index: number, selected: TabbedSelectItem): void {
    const it = selected.raw || this.activeItemsList.find(i => i.id === Number(selected.id));
    if (it) {
      this.items[index].itemId = it.id;
      this.items[index].description = it.description || selected.title;
      const dbCost = it.cost || 0;
      const dbPrice = it.price || 0;
      this.items[index].dbCost = dbCost;
      this.items[index].dbPrice = dbPrice;
      this.items[index].price = dbCost > 0 ? dbCost : (dbPrice > 0 ? dbPrice : Number(selected.price || 0));
      this.items[index].taxRate = it.taxRate || 0;
    } else {
      this.items[index].itemId = Number(selected.id);
      this.items[index].description = selected.title;
      this.items[index].price = Number(selected.price || 0);
    }
  }

  onItemClear(index: number): void {
    this.items[index].itemId = 0;
    this.items[index].description = '';
    this.items[index].price = 0;
    this.items[index].dbCost = 0;
    this.items[index].dbPrice = 0;
  }

  onManualItemLookup(index: number): void {
    const id = this.items[index].itemId;
    if (!id || id <= 0) return;

    this.items[index].isLoadingDb = true;
    this.api.getItemById(id).subscribe({
      next: (item) => {
        this.items[index].isLoadingDb = false;
        if (item) {
          this.items[index].description = item.description;
          const dbCost = item.cost || 0;
          const dbPrice = item.price || 0;
          this.items[index].dbCost = dbCost;
          this.items[index].dbPrice = dbPrice;
          this.items[index].price = dbCost > 0 ? dbCost : (dbPrice > 0 ? dbPrice : 0);
          this.toast.info(`تم جلب بيانات وسعر الصنف (${item.description}) من قاعدة البيانات تلقائياً.`);
        }
      },
      error: () => {
        this.items[index].isLoadingDb = false;
      }
    });
  }

  addItemRow(): void {
    this.items.push({
      itemId: 0,
      description: '',
      qty: 1,
      price: 0,
      storeId: this.defaultStoreId || 1,
      taxRate: 0,
      dbCost: 0,
      dbPrice: 0
    });
  }

  removeItemRow(index: number): void {
    if (this.items.length > 1) {
      this.items.splice(index, 1);
    }
  }

  get totalAmount(): number {
    return this.items.reduce((sum, item) => {
      const line = (item.qty || 0) * (item.price || 0);
      const tax = line * ((item.taxRate || 0) / 100);
      return sum + line + tax;
    }, 0);
  }

  submitPurchases(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (this.items.some(i => !i.itemId || i.qty <= 0)) {
      this.errorMessage = 'يرجى التأكد من اختيار صنف صالح وتحديد كمية أكبر من صفر لجميع البنود.';
      this.toast.error(this.errorMessage);
      return;
    }

    const payload: PurchaseLineDto[] = this.items.map(i => ({
      itemId: Number(i.itemId),
      description: i.description?.trim(),
      qty: Number(i.qty),
      price: Number(i.price),
      storeId: Number(i.storeId || this.defaultStoreId || 1),
      taxRate: Number(i.taxRate || 0)
    }));

    this.submitting = true;
    this.api.addPurchases(this.worksheetId, payload).subscribe({
      next: (res) => {
        this.submitting = false;
        this.successMessage = 'تمت إضافة بنود أمر الشراء بنجاح!';
        this.toast.success(this.successMessage);
        setTimeout(() => {
          this.router.navigate(['/worksheets', this.worksheetId]);
        }, 800);
      },
      error: (err) => {
        this.submitting = false;
        console.error('Error adding purchases', err);
        this.errorMessage = err.error?.message || err.message || 'فشل في حفظ البنود.';
        this.toast.error(this.errorMessage);
      }
    });
  }
}
