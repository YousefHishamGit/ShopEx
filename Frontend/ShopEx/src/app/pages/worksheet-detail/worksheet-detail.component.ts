import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { Worksheet, StoreOption } from '../../models/models';

@Component({
  selector: 'app-worksheet-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './worksheet-detail.component.html'
})
export class WorksheetDetailComponent implements OnInit {
  worksheetId!: number;
  worksheet: Worksheet | null = null;
  loading: boolean = true;
  errorMessage: string = '';
  successMessage: string = '';

  // Approve confirmation dialog
  showApproveModal: boolean = false;
  approving: boolean = false;

  // Destination branch chosen at closing time ('' = keep each line's own branch)
  stores: StoreOption[] = [];
  destinationStoreId: number | null = null;

  constructor(private api: ApiService, private route: ActivatedRoute) {}

  storeName(id?: number | null): string {
    if (id == null) return '—';
    return this.stores.find(s => s.storeId === id)?.storeName || `فرع #${id}`;
  }

  /** Quantity per receiving branch, reflecting the branch picked in the dialog. */
  get branchSummary(): { storeId: number; name: string; lines: number; qty: number }[] {
    const map = new Map<number, { storeId: number; name: string; lines: number; qty: number }>();
    for (const po of this.worksheet?.purchaseOrders || []) {
      const sid = this.destinationStoreId ?? po.storeID ?? 0;
      const row = map.get(sid) || { storeId: sid, name: this.storeName(sid || null), lines: 0, qty: 0 };
      row.lines++;
      row.qty += po.quantityOrdered || 0;
      map.set(sid, row);
    }
    return Array.from(map.values());
  }

  get canApprove(): boolean {
    return !!this.worksheet?.purchaseOrders?.length && this.branchSummary.every(b => b.storeId > 0);
  }

  ngOnInit(): void {
    this.api.getStores().subscribe({ next: d => this.stores = d || [] });
    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      if (idParam) {
        this.worksheetId = parseInt(idParam, 10);
        this.fetchWorksheet();
      }
    });
  }

  fetchWorksheet(): void {
    this.loading = true;
    this.errorMessage = '';
    this.api.getWorksheetById(this.worksheetId).subscribe({
      next: (data) => {
        this.worksheet = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching worksheet', err);
        this.errorMessage = `Failed to load worksheet #${this.worksheetId}.`;
        this.loading = false;
      }
    });
  }

  openApproveModal(): void {
    this.errorMessage = '';
    this.destinationStoreId = null;
    this.showApproveModal = true;
  }

  closeApproveModal(): void {
    this.showApproveModal = false;
  }

  confirmApprove(): void {
    this.approving = true;
    this.api.approveWorksheet(this.worksheetId, this.destinationStoreId).subscribe({
      next: (res) => {
        this.approving = false;
        this.showApproveModal = false;
        this.successMessage = `تم إقفال أمر الشراء #${this.worksheetId} واعتماده، وصرف الكميات من المخزن الرئيسي إلى الفرع المحدد.`;
        this.fetchWorksheet();
        setTimeout(() => this.successMessage = '', 4000);
      },
      error: (err) => {
        this.approving = false;
        console.error('Error approving worksheet', err);
        this.errorMessage = err.error?.message || err.message || 'Failed to approve worksheet.';
      }
    });
  }
}

