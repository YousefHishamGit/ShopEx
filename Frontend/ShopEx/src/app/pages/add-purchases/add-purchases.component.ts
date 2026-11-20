import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { PurchaseLineDto } from '../../models/models';

@Component({
  selector: 'app-add-purchases',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './add-purchases.component.html'
})
export class AddPurchasesComponent implements OnInit {
  worksheetId!: number;
  items: PurchaseLineDto[] = [
    { itemId: 0, description: '', qty: 1, price: 0, storeId: 1, taxRate: 0 }
  ];

  submitting: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  constructor(
    private api: ApiService,
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

  submitPurchases(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (this.items.some(i => !i.itemId || i.qty <= 0)) {
      this.errorMessage = 'Please provide a valid Item ID and quantity > 0 for all lines.';
      return;
    }

    const payload: PurchaseLineDto[] = this.items.map(i => ({
      itemId: Number(i.itemId),
      description: i.description?.trim(),
      qty: Number(i.qty),
      price: Number(i.price),
      storeId: Number(i.storeId),
      taxRate: Number(i.taxRate || 0)
    }));

    this.submitting = true;
    this.api.addPurchases(this.worksheetId, payload).subscribe({
      next: (res) => {
        this.submitting = false;
        this.successMessage = 'Purchase order lines added successfully!';
        setTimeout(() => {
          this.router.navigate(['/worksheets', this.worksheetId]);
        }, 1000);
      },
      error: (err) => {
        this.submitting = false;
        console.error('Error adding purchases', err);
        this.errorMessage = err.error?.message || err.message || 'Failed to add purchases.';
      }
    });
  }
}

