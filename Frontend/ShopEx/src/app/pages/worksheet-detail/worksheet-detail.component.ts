import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { Worksheet } from '../../models/models';

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

  constructor(private api: ApiService, private route: ActivatedRoute) {}

  ngOnInit(): void {
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
    this.showApproveModal = true;
  }

  closeApproveModal(): void {
    this.showApproveModal = false;
  }

  confirmApprove(): void {
    this.approving = true;
    this.api.approveWorksheet(this.worksheetId).subscribe({
      next: (res) => {
        this.approving = false;
        this.showApproveModal = false;
        this.successMessage = `Worksheet #${this.worksheetId} approved successfully (Status changed to 2 - Approved)!`;
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

