import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ExpenseService } from '../../services/expense.service';
import {
  ExpenseCategoryDto,
  CreateExpenseCategoryRequest,
  UpdateExpenseCategoryRequest,
  CreateExpenseRequest,
  UpdateExpenseRequest,
  ExpenseResponse,
  ExpenseSummaryDto,
  ExpenseByCategorySummaryDto,
  ExpensePaymentMethod
} from '../../models/models';

@Component({
  selector: 'app-expenses',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './expenses.component.html'
})
export class ExpensesComponent implements OnInit {
  activeTab: 'expenses' | 'categories' = 'expenses';

  // Stores
  stores: any[] = [];

  // Categories
  categories: ExpenseCategoryDto[] = [];
  allCategories: ExpenseCategoryDto[] = [];

  // Expenses
  expenses: ExpenseResponse[] = [];
  summary: ExpenseSummaryDto | null = null;

  // Filters
  filters = {
    storeId: undefined as number | undefined,
    categoryId: undefined as number | undefined,
    fromDate: '',
    toDate: '',
    paymentMethod: undefined as number | undefined
  };

  // Loading
  loading = false;
  summaryLoading = false;

  // ========== Expense Modal ==========
  showExpenseModal = false;
  editingExpense: ExpenseResponse | null = null;
  expenseForm: CreateExpenseRequest = this.getEmptyExpenseForm();

  // Category autocomplete
  categorySearch = '';
  categoryDropdownOpen = false;
  filteredCategories: ExpenseCategoryDto[] = [];

  // ========== Category Modal ==========
  showCategoryModal = false;
  editingCategory: ExpenseCategoryDto | null = null;
  categoryForm: CreateExpenseCategoryRequest = { name: '', description: '' };

  // ========== Delete Confirm ==========
  showDeleteConfirm = false;
  deletingExpenseId: number | null = null;

  // ========== Misc ==========
  errorMsg = '';
  successMsg = '';

  paymentMethods = [
    { value: ExpensePaymentMethod.Cash, label: 'نقدي' },
    { value: ExpensePaymentMethod.BankTransfer, label: 'تحويل بنكي' },
    { value: ExpensePaymentMethod.CreditCard, label: 'بطاقة ائتمان' },
    { value: ExpensePaymentMethod.ElectronicWallet, label: 'محفظة إلكترونية' },
    { value: ExpensePaymentMethod.Cheque, label: 'شيك' }
  ];

  constructor(private expenseService: ExpenseService) {}

  ngOnInit(): void {
    this.loadInitialData();
  }

  loadInitialData(): void {
    this.expenseService.getStores().subscribe({
      next: s => this.stores = s,
      error: () => {}
    });
    this.expenseService.getCategories(true).subscribe({
      next: cats => {
        this.allCategories = cats;
        this.categories = cats;
      },
      error: () => {}
    });
    this.loadExpenses();
  }

  loadExpenses(): void {
    this.loading = true;
    this.summaryLoading = true;
    this.expenseService.getSummary(this.getActiveFilters()).subscribe({
      next: s => {
        this.summary = s;
        this.expenses = s.items;
        this.loading = false;
        this.summaryLoading = false;
      },
      error: () => {
        this.loading = false;
        this.summaryLoading = false;
      }
    });
  }

  getActiveFilters() {
    return {
      storeId: this.filters.storeId,
      categoryId: this.filters.categoryId,
      fromDate: this.filters.fromDate || undefined,
      toDate: this.filters.toDate || undefined,
      paymentMethod: this.filters.paymentMethod
    };
  }

  applyFilters(): void {
    this.loadExpenses();
  }

  clearFilters(): void {
    this.filters = { storeId: undefined, categoryId: undefined, fromDate: '', toDate: '', paymentMethod: undefined };
    this.loadExpenses();
  }

  // ========== Expense CRUD ==========
  openCreateExpense(): void {
    this.editingExpense = null;
    this.expenseForm = this.getEmptyExpenseForm();
    this.categorySearch = '';
    this.filteredCategories = [];
    this.showExpenseModal = true;
    this.errorMsg = '';
  }

  openEditExpense(e: ExpenseResponse): void {
    this.editingExpense = e;
    this.expenseForm = {
      categoryId: e.categoryId,
      categoryName: undefined,
      storeId: e.storeId,
      amount: e.amount,
      expenseDate: e.expenseDate ? e.expenseDate.substring(0, 10) : '',
      paymentMethod: e.paymentMethod,
      recipient: e.recipient || '',
      receiptNumber: e.receiptNumber || '',
      notes: e.notes || '',
      createdBy: e.createdBy || ''
    };
    this.categorySearch = e.categoryName;
    this.filteredCategories = [];
    this.showExpenseModal = true;
    this.errorMsg = '';
  }

  saveExpense(): void {
    if (!this.expenseForm.amount || this.expenseForm.amount <= 0) {
      this.errorMsg = 'يرجى إدخال مبلغ صحيح';
      return;
    }
    if (!this.expenseForm.categoryId && !this.expenseForm.categoryName?.trim()) {
      this.errorMsg = 'يرجى اختيار أو كتابة بند المصروف';
      return;
    }

    this.loading = true;
    if (this.editingExpense) {
      const req: UpdateExpenseRequest = {
        categoryId: this.expenseForm.categoryId!,
        storeId: this.expenseForm.storeId,
        amount: this.expenseForm.amount,
        expenseDate: this.expenseForm.expenseDate || new Date().toISOString().substring(0, 10),
        paymentMethod: this.expenseForm.paymentMethod,
        recipient: this.expenseForm.recipient,
        receiptNumber: this.expenseForm.receiptNumber,
        notes: this.expenseForm.notes
      };
      this.expenseService.update(this.editingExpense.id, req).subscribe({
        next: () => { this.showExpenseModal = false; this.loadExpenses(); this.loadCategories(); },
        error: err => { this.errorMsg = err?.error?.message || 'حدث خطأ'; this.loading = false; }
      });
    } else {
      this.expenseService.create(this.expenseForm).subscribe({
        next: () => { this.showExpenseModal = false; this.loadExpenses(); this.loadCategories(); },
        error: err => { this.errorMsg = err?.error?.message || 'حدث خطأ'; this.loading = false; }
      });
    }
  }

  confirmDeleteExpense(id: number): void {
    this.deletingExpenseId = id;
    this.showDeleteConfirm = true;
  }

  deleteExpense(): void {
    if (!this.deletingExpenseId) return;
    this.expenseService.delete(this.deletingExpenseId).subscribe({
      next: () => { this.showDeleteConfirm = false; this.deletingExpenseId = null; this.loadExpenses(); },
      error: () => { this.showDeleteConfirm = false; }
    });
  }

  // ========== Category Autocomplete ==========
  onCategorySearchInput(): void {
    const q = this.categorySearch.trim().toLowerCase();
    if (!q) {
      this.filteredCategories = this.allCategories.filter(c => c.isActive);
      this.expenseForm.categoryId = undefined;
      this.expenseForm.categoryName = undefined;
    } else {
      this.filteredCategories = this.allCategories.filter(c => c.isActive && c.name.toLowerCase().includes(q));
      // Check exact match
      const exact = this.allCategories.find(c => c.name.toLowerCase() === q);
      if (exact) {
        this.expenseForm.categoryId = exact.id;
        this.expenseForm.categoryName = undefined;
      } else {
        this.expenseForm.categoryId = undefined;
        this.expenseForm.categoryName = this.categorySearch.trim();
      }
    }
    this.categoryDropdownOpen = true;
  }

  selectCategory(cat: ExpenseCategoryDto): void {
    this.categorySearch = cat.name;
    this.expenseForm.categoryId = cat.id;
    this.expenseForm.categoryName = undefined;
    this.categoryDropdownOpen = false;
    this.filteredCategories = [];
  }

  onCategoryFocus(): void {
    this.filteredCategories = this.allCategories.filter(c => c.isActive);
    this.categoryDropdownOpen = true;
  }

  closeCategoryDropdown(): void {
    setTimeout(() => { this.categoryDropdownOpen = false; }, 200);
  }

  // ========== Category CRUD ==========
  loadCategories(): void {
    this.expenseService.getCategories(true).subscribe({
      next: cats => { this.allCategories = cats; this.categories = cats; },
      error: () => {}
    });
  }

  openCreateCategory(): void {
    this.editingCategory = null;
    this.categoryForm = { name: '', description: '' };
    this.showCategoryModal = true;
    this.errorMsg = '';
  }

  openEditCategory(cat: ExpenseCategoryDto): void {
    this.editingCategory = cat;
    this.categoryForm = { name: cat.name, description: cat.description || '' };
    this.showCategoryModal = true;
    this.errorMsg = '';
  }

  saveCategory(): void {
    if (!this.categoryForm.name?.trim()) {
      this.errorMsg = 'يرجى إدخال اسم البند';
      return;
    }
    if (this.editingCategory) {
      const req: UpdateExpenseCategoryRequest = {
        name: this.categoryForm.name,
        description: this.categoryForm.description,
        isActive: this.editingCategory.isActive
      };
      this.expenseService.updateCategory(this.editingCategory.id, req).subscribe({
        next: () => { this.showCategoryModal = false; this.loadCategories(); },
        error: err => { this.errorMsg = err?.error?.message || 'حدث خطأ'; }
      });
    } else {
      this.expenseService.createCategory(this.categoryForm).subscribe({
        next: () => { this.showCategoryModal = false; this.loadCategories(); },
        error: err => { this.errorMsg = err?.error?.message || 'حدث خطأ'; }
      });
    }
  }

  toggleCategoryStatus(cat: ExpenseCategoryDto): void {
    this.expenseService.toggleCategory(cat.id).subscribe({
      next: () => this.loadCategories(),
      error: () => {}
    });
  }

  // ========== Helpers ==========
  getEmptyExpenseForm(): CreateExpenseRequest {
    return {
      categoryId: undefined,
      categoryName: undefined,
      storeId: undefined,
      amount: 0,
      expenseDate: new Date().toISOString().substring(0, 10),
      paymentMethod: ExpensePaymentMethod.Cash,
      recipient: '',
      receiptNumber: '',
      notes: '',
      createdBy: ''
    };
  }

  getPaymentMethodLabel(pm: number): string {
    return this.paymentMethods.find(p => p.value === pm)?.label || '';
  }

  getStoreName(id?: number): string {
    if (!id) return '-';
    return this.stores.find(s => s.id === id)?.name || id.toString();
  }

  formatAmount(n: number): string {
    return n.toLocaleString('ar-EG', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatDate(d: string): string {
    if (!d) return '-';
    return new Date(d).toLocaleDateString('ar-EG');
  }

  get topCategory(): ExpenseByCategorySummaryDto | null {
    if (!this.summary?.categorySummaries?.length) return null;
    return this.summary.categorySummaries.reduce((a, b) => a.totalAmount > b.totalAmount ? a : b);
  }

  closeExpenseModal(): void { this.showExpenseModal = false; }
  closeCategoryModal(): void { this.showCategoryModal = false; }
  closeDeleteConfirm(): void { this.showDeleteConfirm = false; this.deletingExpenseId = null; }
}
