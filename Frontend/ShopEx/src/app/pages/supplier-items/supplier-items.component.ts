import { Component, OnInit, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { SupplierItem } from '../../models/models';

@Component({
  selector: 'app-supplier-items',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './supplier-items.component.html'
})
export class SupplierItemsComponent implements OnInit {
  @Input() id!: string; // supplierId from route
  supplierId!: number;
  items: SupplierItem[] = [];
  filteredItems: SupplierItem[] = [];
  searchTerm: string = '';
  loading: boolean = true;
  errorMessage: string = '';

  selectedItems = new Set<number>();

  constructor(
    private api: ApiService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      if (idParam) {
        this.supplierId = parseInt(idParam, 10);
        this.fetchItems();
      }
    });
  }

  fetchItems(): void {
    this.loading = true;
    this.errorMessage = '';
    this.api.getSupplierItems(this.supplierId).subscribe({
      next: (data) => {
        this.items = data || [];
        this.applyFilter();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching supplier items', err);
        this.errorMessage = `Failed to load items for supplier #${this.supplierId}.`;
        this.loading = false;
      }
    });
  }

  applyFilter(): void {
    const term = this.searchTerm.toLowerCase().trim();
    if (!term) {
      this.filteredItems = [...this.items];
    } else {
      this.filteredItems = this.items.filter(item =>
        (item.description && item.description.toLowerCase().includes(term)) ||
        (item.itemLookupCode && item.itemLookupCode.toLowerCase().includes(term)) ||
        (item.id && item.id.toString().includes(term))
      );
    }
  }

  toggleSelect(itemId: number): void {
    if (this.selectedItems.has(itemId)) {
      this.selectedItems.delete(itemId);
    } else {
      this.selectedItems.add(itemId);
    }
  }

  toggleSelectAll(checked: boolean): void {
    if (checked) {
      this.filteredItems.forEach(i => this.selectedItems.add(i.id));
    } else {
      this.selectedItems.clear();
    }
  }

  get isAllSelected(): boolean {
    return this.filteredItems.length > 0 && this.filteredItems.every(i => this.selectedItems.has(i.id));
  }

  createInvoiceWithSelected(): void {
    const selected = this.items.filter(i => this.selectedItems.has(i.id));
    this.router.navigate(['/invoices/new'], {
      state: {
        supplierId: this.supplierId,
        items: selected
      }
    });
  }
}

