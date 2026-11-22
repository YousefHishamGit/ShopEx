import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription, debounceTime } from 'rxjs';
import { ExpenseService } from '../../services/expense.service';
import { ToastService } from '../../services/toast.service';
import {
  ExpenseCategoryDto,
  CreateExpenseCategoryRequest,
  UpdateExpenseCategoryRequest,
  CreateExpenseRequest,
  UpdateExpenseRequest,
  ExpenseResponse,
  ExpenseSummaryDto,
  ExpenseByCategorySummaryDto,
  ExpenseFilters,
  ExpensePaymentMethod
} from '../../models/models';

type DatePreset = 'all' | 'today' | 'month' | 'lastMonth';

@Component({
  selector: 'app-expenses',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './expenses.component.html'
})
export class ExpensesComponent implements OnInit, OnDestroy {
  activeTab: 'expenses' | 'categories' = 'expenses';

  // Reference data
  stores: { id: number; name: string }[] = [];
  private storesMap = new Map<number, string>();
  allCategories: ExpenseCategoryDto[] = [];

  // Expenses (current page) + summary over ALL matching rows
  expenses: ExpenseResponse[] = [];
  summary: ExpenseSummaryDto | null = null;
  topCategory: ExpenseByCategorySummaryDto | null = null;
  averageAmount = 0;
  activeCategoriesCount = 0;

  // Paging
  page = 1;
  readonly pageSize = 25;

  // Filters
  filters = {
    storeId: undefined as number | undefined,
    categoryId: undefined as number | undefined,
    fromDate: '',
    toDate: '',
    paymentMethod: undefined as number | undefined,
    search: ''
  };
  datePreset: DatePreset = 'month';

  loading = false;
  private loadSub?: Subscription;
  private searchInput$ = new Subject<void>();
  private searchSub?: Subscription;

  // ========== Expense Modal ==========
  showExpenseModal = false;
  submittingExpense = false;
  editingExpense: ExpenseResponse | null = null;
  expenseForm: CreateExpenseRequest = this.getEmptyExpenseForm();

  // Category autocomplete
  categorySearch = '';
  categoryDropdownOpen = false;
  filteredCategories: ExpenseCategoryDto[] = [];

  // ========== Category Modal ==========
  showCategoryModal = false;
  submittingCategory = false;
  editingCategory: ExpenseCategoryDto | null = null;
  categoryForm: CreateExpenseCategoryRequest = { name: '', description: '' };

  // ========== Delete Confirm ==========
  showDeleteConfirm = false;
  deleting = false;
  deletingExpenseId: number | null = null;

  errorMsg = '';

  readonly paymentMethods = [
    { value: ExpensePaymentMethod.Cash, label: 'نقدي', icon: 'payments' },
    { value: ExpensePaymentMethod.BankTransfer, label: 'تحويل بنكي', icon: 'account_balance' },
    { value: ExpensePaymentMethod.CreditCard, label: 'بطاقة ائتمان', icon: 'credit_card' },
    { value: ExpensePaymentMethod.ElectronicWallet, label: 'محفظة إلكترونية', icon: 'account_balance_wallet' },
    { value: ExpensePaymentMethod.Cheque, label: 'شيك', icon: 'edit_document' }
  ];

  constructor(
    private expenseService: ExpenseService,
    private toast: ToastService
  ) { }

  ngOnInit(): void {
    this.searchSub = this.searchInput$.pipe(debounceTime(350)).subscribe(() => {
      this.page = 1;
      this.loadExpenses();
    });

    this.expenseService.getStores().subscribe({
      next: (s) => {
        this.stores = s || [];
        this.storesMap = new Map(this.stores.map((x) => [x.id, x.name]));
      },
      error: () => { }
    });
    this.loadCategories();
    this.loadExpenses();
  }

  ngOnDestroy(): void {
    this.loadSub?.unsubscribe();
    this.searchSub?.unsubscribe();
  }

  // ========== Loading ==========
  loadExpenses(): void {
    // إلغاء الطلب السابق حتى لا يكتب رد قديم فوق رد أحدث
    this.loadSub?.unsubscribe();
    this.loading = true;

    this.loadSub = this.expenseService.getSummary(this.getActiveFilters(), this.page, this.pageSize).subscribe({
      next: (s) => {
        this.summary = s;
        this.expenses = s.items;
        this.page = s.page;
        // التوزيع مرتب تنازلياً من الـ API، فأول عنصر هو أعلى بند
        this.topCategory = s.categorySummaries?.[0] ?? null;
        this.averageAmount = s.totalCount > 0 ? s.totalAmount / s.totalCount : 0;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.toast.error(err?.error?.message || 'تعذر تحميل المصروفات');
      }
    });
  }

  loadCategories(): void {
    this.expenseService.getCategories(true).subscribe({
      next: (cats) => {
        this.allCategories = cats;
        this.activeCategoriesCount = cats.filter((c) => c.isActive).length;
      },
      error: () => { }
    });
  }

  getActiveFilters(): ExpenseFilters {
    return {
      storeId: this.filters.storeId,
      categoryId: this.filters.categoryId,
      fromDate: this.filters.fromDate || undefined,
      toDate: this.filters.toDate || undefined,
      paymentMethod: this.filters.paymentMethod,
      search: this.filters.search || undefined
    };
  }

  // ========== Filters ==========
  onFilterChange(): void {
    this.page = 1;
    this.loadExpenses();
  }

  onDateChange(): void {
    this.datePreset = 'all';
    // لا نبحث إلا لو الفترة منطقية
    if (this.filters.fromDate && this.filters.toDate && this.filters.fromDate > this.filters.toDate) {
      return;
    }
    this.onFilterChange();
  }

  onSearchInput(): void {
    this.searchInput$.next();
  }

  applyPreset(preset: DatePreset): void {
    const now = new Date();
    this.datePreset = preset;

    if (preset === 'today') {
      this.filters.fromDate = this.toDateStr(now);
      this.filters.toDate = this.toDateStr(now);
    } else if (preset === 'month') {
      this.filters.fromDate = this.toDateStr(new Date(now.getFullYear(), now.getMonth(), 1));
      this.filters.toDate = this.toDateStr(now);
    } else if (preset === 'lastMonth') {
      this.filters.fromDate = this.toDateStr(new Date(now.getFullYear(), now.getMonth() - 1, 1));
      this.filters.toDate = this.toDateStr(new Date(now.getFullYear(), now.getMonth(), 0));
    } else {
      this.filters.fromDate = '';
      this.filters.toDate = '';
    }
    this.onFilterChange();
  }

  clearFilters(): void {
    this.filters = { storeId: undefined, categoryId: undefined, fromDate: '', toDate: '', paymentMethod: undefined, search: '' };
    this.datePreset = 'all';
    this.onFilterChange();
  }

  get hasActiveFilters(): boolean {
    const f = this.filters;
    return !!(f.storeId || f.categoryId || f.fromDate || f.toDate || f.paymentMethod || f.search.trim());
  }

  /** فلترة على بند معين بالضغط على شريط التوزيع */
  filterByCategory(categoryId: number): void {
    this.filters.categoryId = this.filters.categoryId === categoryId ? undefined : categoryId;
    this.onFilterChange();
  }

  // ========== Paging ==========
  goToPage(p: number): void {
    const total = this.summary?.totalPages ?? 1;
    if (p < 1 || p > total || p === this.page) return;
    this.page = p;
    this.loadExpenses();
  }

  get pageFrom(): number {
    return this.summary && this.summary.totalCount > 0 ? (this.page - 1) * this.pageSize + 1 : 0;
  }

  get pageTo(): number {
    return this.summary ? Math.min(this.page * this.pageSize, this.summary.totalCount) : 0;
  }

  // ========== Expense CRUD ==========
  openCreateExpense(): void {
    this.editingExpense = null;
    this.expenseForm = this.getEmptyExpenseForm();
    this.categorySearch = '';
    this.filteredCategories = [];
    this.errorMsg = '';
    this.showExpenseModal = true;
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
    this.errorMsg = '';
    this.showExpenseModal = true;
  }

  closeExpenseModal(): void {
    if (this.submittingExpense) return;
    this.showExpenseModal = false;
  }

  /** هل البند المكتوب يطابق بنداً معطلاً؟ */
  get selectedCategoryInactive(): boolean {
    const c = this.allCategories.find((x) => x.id === this.expenseForm.categoryId);
    // مسموح لو المصروف المعدَّل يستخدم نفس البند أصلاً
    return !!c && !c.isActive && c.id !== this.editingExpense?.categoryId;
  }

  saveExpense(): void {
    if (this.submittingExpense) return;

    if (!this.expenseForm.amount || this.expenseForm.amount <= 0) {
      this.errorMsg = 'يرجى إدخال مبلغ صحيح';
      return;
    }
    if (!this.expenseForm.categoryId && !this.expenseForm.categoryName?.trim()) {
      this.errorMsg = 'يرجى اختيار أو كتابة بند المصروف';
      return;
    }
    if (this.selectedCategoryInactive) {
      this.errorMsg = 'هذا البند معطل، فعّله أولاً أو اختر بنداً آخر';
      return;
    }

    this.submittingExpense = true;
    this.errorMsg = '';

    const done = (msg: string) => {
      this.submittingExpense = false;
      this.showExpenseModal = false;
      this.toast.success(msg);
      this.loadExpenses();
      this.loadCategories(); // قد يكون أُنشئ بند جديد
    };
    const fail = (err: any, fallback: string) => {
      this.submittingExpense = false;
      this.errorMsg = err?.error?.message || fallback;
      this.toast.error(this.errorMsg);
    };

    if (this.editingExpense) {
      const req: UpdateExpenseRequest = {
        categoryId: this.expenseForm.categoryId,
        categoryName: this.expenseForm.categoryId ? undefined : this.expenseForm.categoryName?.trim(),
        storeId: this.expenseForm.storeId,
        amount: Number(this.expenseForm.amount),
        expenseDate: this.expenseForm.expenseDate || this.toDateStr(new Date()),
        paymentMethod: this.expenseForm.paymentMethod,
        recipient: this.expenseForm.recipient,
        receiptNumber: this.expenseForm.receiptNumber,
        notes: this.expenseForm.notes
      };
      this.expenseService.update(this.editingExpense.id, req).subscribe({
        next: () => done('تم تحديث بيانات المصروف بنجاح!'),
        error: (err) => fail(err, 'حدث خطأ أثناء تعديل المصروف')
      });
    } else {
      this.expenseService.create({ ...this.expenseForm, amount: Number(this.expenseForm.amount) }).subscribe({
        next: () => done('تم تسجيل المصروف بنجاح!'),
        error: (err) => fail(err, 'حدث خطأ أثناء تسجيل المصروف')
      });
    }
  }

  confirmDeleteExpense(id: number): void {
    this.deletingExpenseId = id;
    this.showDeleteConfirm = true;
  }

  deleteExpense(): void {
    if (!this.deletingExpenseId || this.deleting) return;
    this.deleting = true;
    this.expenseService.delete(this.deletingExpenseId).subscribe({
      next: () => {
        this.deleting = false;
        this.showDeleteConfirm = false;
        this.deletingExpenseId = null;
        this.toast.success('تم حذف المصروف بنجاح.');
        // لو حذفنا آخر عنصر في الصفحة نرجع صفحة للخلف (الـ API يصحح الصفحة تلقائياً أيضاً)
        this.loadExpenses();
        this.loadCategories();
      },
      error: () => {
        this.deleting = false;
        this.showDeleteConfirm = false;
        this.toast.error('تعذر حذف المصروف');
      }
    });
  }

  closeDeleteConfirm(): void {
    if (this.deleting) return;
    this.showDeleteConfirm = false;
    this.deletingExpenseId = null;
  }

  // ========== Category Autocomplete ==========
  onCategorySearchInput(): void {
    const q = this.categorySearch.trim().toLowerCase();
    const active = this.allCategories.filter((c) => c.isActive);

    if (!q) {
      this.filteredCategories = active;
      this.expenseForm.categoryId = undefined;
      this.expenseForm.categoryName = undefined;
    } else {
      this.filteredCategories = active.filter((c) => c.name.toLowerCase().includes(q));
      const exact = this.allCategories.find((c) => c.name.toLowerCase() === q);
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
    this.filteredCategories = this.allCategories.filter((c) => c.isActive);
    this.categoryDropdownOpen = true;
  }

  closeCategoryDropdown(): void {
    setTimeout(() => { this.categoryDropdownOpen = false; }, 200);
  }

  // ========== Category CRUD ==========
  openCreateCategory(): void {
    this.editingCategory = null;
    this.categoryForm = { name: '', description: '' };
    this.errorMsg = '';
    this.showCategoryModal = true;
  }

  openEditCategory(cat: ExpenseCategoryDto): void {
    this.editingCategory = cat;
    this.categoryForm = { name: cat.name, description: cat.description || '' };
    this.errorMsg = '';
    this.showCategoryModal = true;
  }

  closeCategoryModal(): void {
    if (this.submittingCategory) return;
    this.showCategoryModal = false;
  }

  saveCategory(): void {
    if (this.submittingCategory) return;

    if (!this.categoryForm.name?.trim()) {
      this.errorMsg = 'يرجى إدخال اسم البند';
      return;
    }

    this.submittingCategory = true;
    this.errorMsg = '';

    const done = (msg: string) => {
      this.submittingCategory = false;
      this.showCategoryModal = false;
      this.toast.success(msg);
      this.loadCategories();
      // اسم البند قد يتغير في جدول الحركات
      if (this.editingCategory) this.loadExpenses();
    };
    const fail = (err: any) => {
      this.submittingCategory = false;
      this.errorMsg = err?.error?.message || 'حدث خطأ';
      this.toast.error(this.errorMsg);
    };

    if (this.editingCategory) {
      const req: UpdateExpenseCategoryRequest = {
        name: this.categoryForm.name,
        description: this.categoryForm.description,
        isActive: this.editingCategory.isActive
      };
      this.expenseService.updateCategory(this.editingCategory.id, req).subscribe({
        next: () => done('تم تعديل البند بنجاح'),
        error: fail
      });
    } else {
      this.expenseService.createCategory(this.categoryForm).subscribe({
        next: () => done('تمت إضافة البند بنجاح'),
        error: fail
      });
    }
  }

  toggleCategoryStatus(cat: ExpenseCategoryDto): void {
    this.expenseService.toggleCategory(cat.id).subscribe({
      next: (r) => {
        this.toast.success(r.isActive ? 'تم تفعيل البند' : 'تم تعطيل البند');
        this.loadCategories();
      },
      error: (err) => this.toast.error(err?.error?.message || 'تعذر تغيير حالة البند')
    });
  }

  // ========== Helpers ==========
  getEmptyExpenseForm(): CreateExpenseRequest {
    return {
      categoryId: undefined,
      categoryName: undefined,
      storeId: undefined,
      amount: 0,
      expenseDate: this.toDateStr(new Date()),
      paymentMethod: ExpensePaymentMethod.Cash,
      recipient: '',
      receiptNumber: '',
      notes: '',
      createdBy: ''
    };
  }

  /** yyyy-MM-dd بالتوقيت المحلي (toISOString يعطي UTC وقد يرجع يوماً للخلف) */
  private toDateStr(d: Date): string {
    const m = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${d.getFullYear()}-${m}-${day}`;
  }

  getPaymentMethod(pm: number) {
    return this.paymentMethods.find((p) => p.value === pm);
  }

  getStoreName(id?: number | null): string {
    if (!id) return 'الإدارة العامة';
    return this.storesMap.get(id) || `فرع #${id}`;
  }

  trackExpense = (_: number, e: ExpenseResponse) => e.id;
  trackCategory = (_: number, c: ExpenseCategoryDto) => c.id;
}
