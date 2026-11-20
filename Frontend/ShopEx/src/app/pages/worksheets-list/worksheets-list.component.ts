import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { Worksheet } from '../../models/models';

@Component({
  selector: 'app-worksheets-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './worksheets-list.component.html'
})
export class WorksheetsListComponent implements OnInit {
  worksheets: Worksheet[] = [];
  filteredWorksheets: Worksheet[] = [];
  searchTerm: string = '';
  loading: boolean = true;
  errorMessage: string = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.fetchWorksheets();
  }

  fetchWorksheets(): void {
    this.loading = true;
    this.errorMessage = '';
    this.api.getWorksheets().subscribe({
      next: (data) => {
        this.worksheets = data || [];
        this.applyFilter();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching worksheets', err);
        this.errorMessage = 'Failed to load worksheets. Ensure backend is running.';
        this.loading = false;
      }
    });
  }

  applyFilter(): void {
    const term = this.searchTerm.toLowerCase().trim();
    if (!term) {
      this.filteredWorksheets = [...this.worksheets];
    } else {
      this.filteredWorksheets = this.worksheets.filter(w =>
        (w.title && w.title.toLowerCase().includes(term)) ||
        (w.id && w.id.toString().includes(term))
      );
    }
  }
}

