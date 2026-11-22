import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../services/user.service';
import { BalanceService } from '../../services/balance.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { AppUser, CreateUserRequest, StoreOption, UserRole } from '../../models/models';

type RoleFilter = 'all' | UserRole;

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.component.html'
})
export class UsersComponent implements OnInit {
  readonly UserRole = UserRole;

  users: AppUser[] = [];
  filteredUsers: AppUser[] = [];
  stores: StoreOption[] = [];

  loading = false;
  errorMessage = '';

  searchQuery = '';
  roleFilter: RoleFilter = 'all';

  // Add user modal
  showAddModal = false;
  submittingAdd = false;
  addError = '';
  showAddPassword = false;
  addForm: CreateUserRequest = this.emptyForm();

  // Change password modal
  passwordTarget: AppUser | null = null;
  newPassword = '';
  showNewPassword = false;
  submittingPassword = false;
  passwordError = '';

  // Delete confirmation modal
  deleteTarget: AppUser | null = null;
  submittingDelete = false;

  // Row currently being enabled/disabled
  togglingId: number | null = null;

  constructor(
    private userService: UserService,
    private balanceService: BalanceService,
    private auth: AuthService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
    this.balanceService.getStores().subscribe({
      next: (data) => (this.stores = data || []),
      error: () => this.toast.warning('تعذر تحميل قائمة الفروع.')
    });
  }

  get currentUserId(): number | null {
    return this.auth.currentUser()?.id ?? null;
  }

  // ------------------------------------------------------------
  // List
  // ------------------------------------------------------------
  loadUsers(): void {
    this.loading = true;
    this.errorMessage = '';
    this.userService.getAll().subscribe({
      next: (data) => {
        this.users = data || [];
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'تعذر تحميل المستخدمين.';
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    const q = this.searchQuery.trim().toLowerCase();
    this.filteredUsers = this.users.filter((u) => {
      if (this.roleFilter !== 'all' && u.role !== this.roleFilter) return false;
      if (!q) return true;
      return (
        u.name.toLowerCase().includes(q) ||
        u.username.toLowerCase().includes(q) ||
        u.phone.includes(q) ||
        (u.storeName || '').toLowerCase().includes(q)
      );
    });
  }

  get adminsCount(): number {
    return this.users.filter((u) => u.role === UserRole.Admin).length;
  }

  get employeesCount(): number {
    return this.users.filter((u) => u.role === UserRole.Employee).length;
  }

  get disabledCount(): number {
    return this.users.filter((u) => !u.isActive).length;
  }

  trackUser = (_: number, u: AppUser) => u.id;

  // ------------------------------------------------------------
  // Add user
  // ------------------------------------------------------------
  private emptyForm(): CreateUserRequest {
    return { name: '', phone: '', role: UserRole.Employee, username: '', password: '', storeId: null };
  }

  openAddModal(): void {
    this.addForm = this.emptyForm();
    this.addError = '';
    this.showAddPassword = false;
    this.showAddModal = true;
  }

  closeAddModal(): void {
    if (this.submittingAdd) return;
    this.showAddModal = false;
    this.addForm = this.emptyForm(); // never keep a typed password around
  }

  submitAdd(): void {
    if (this.submittingAdd) return;
    const f = this.addForm;

    if (!f.name.trim()) return this.failAdd('يرجى إدخال الاسم.');
    if (!/^[0-9+\-\s()]{7,30}$/.test(f.phone.trim())) return this.failAdd('رقم الهاتف غير صحيح.');
    if (!/^[A-Za-z0-9._-]{3,50}$/.test(f.username.trim())) {
      return this.failAdd('اسم المستخدم من 3 إلى 50 حرف (حروف إنجليزية وأرقام و . _ - فقط).');
    }
    if (f.password.length < 6) return this.failAdd('كلمة المرور يجب ألا تقل عن 6 أحرف.');
    if (Number(f.role) === UserRole.Employee && !f.storeId) return this.failAdd('يجب اختيار الفرع للموظف.');

    this.submittingAdd = true;
    this.addError = '';

    this.userService.create(f).subscribe({
      next: (created) => {
        this.submittingAdd = false;
        this.showAddModal = false;
        this.addForm = this.emptyForm();
        this.toast.success(`تمت إضافة المستخدم (${created.name}) بنجاح.`);
        this.loadUsers();
      },
      error: (err) => {
        this.submittingAdd = false;
        this.addError = err.error?.message || this.firstValidationError(err) || 'حدث خطأ أثناء إضافة المستخدم.';
      }
    });
  }

  private failAdd(message: string): void {
    this.addError = message;
  }

  // ------------------------------------------------------------
  // Change password
  // ------------------------------------------------------------
  openPasswordModal(user: AppUser): void {
    this.passwordTarget = user;
    this.newPassword = '';
    this.passwordError = '';
    this.showNewPassword = false;
  }

  closePasswordModal(): void {
    if (this.submittingPassword) return;
    this.passwordTarget = null;
    this.newPassword = '';
  }

  submitPassword(): void {
    if (!this.passwordTarget || this.submittingPassword) return;

    if (this.newPassword.length < 6) {
      this.passwordError = 'كلمة المرور يجب ألا تقل عن 6 أحرف.';
      return;
    }

    const target = this.passwordTarget;
    this.submittingPassword = true;
    this.passwordError = '';

    this.userService.updatePassword(target.id, this.newPassword).subscribe({
      next: () => {
        this.submittingPassword = false;
        this.passwordTarget = null;
        this.newPassword = '';
        this.toast.success(`تم تغيير كلمة مرور (${target.name}). سيُطلب منه تسجيل الدخول من جديد.`);
        // changing your own password revokes your own session as well
        if (target.id === this.currentUserId) {
          this.auth.logout();
        }
      },
      error: (err) => {
        this.submittingPassword = false;
        this.passwordError = err.error?.message || this.firstValidationError(err) || 'تعذر تغيير كلمة المرور.';
      }
    });
  }

  // ------------------------------------------------------------
  // Enable / disable
  // ------------------------------------------------------------
  toggleActive(user: AppUser): void {
    if (this.togglingId !== null) return;
    this.togglingId = user.id;

    this.userService.setActive(user.id, !user.isActive).subscribe({
      next: (updated) => {
        this.togglingId = null;
        this.users = this.users.map((u) => (u.id === updated.id ? updated : u));
        this.applyFilters();
        this.toast.success(updated.isActive ? `تم تفعيل (${updated.name}).` : `تم تعطيل (${updated.name}).`);
      },
      error: (err) => {
        this.togglingId = null;
        this.toast.error(err.error?.message || 'تعذر تغيير حالة المستخدم.');
      }
    });
  }

  // ------------------------------------------------------------
  // Delete
  // ------------------------------------------------------------
  openDeleteModal(user: AppUser): void {
    this.deleteTarget = user;
  }

  closeDeleteModal(): void {
    if (this.submittingDelete) return;
    this.deleteTarget = null;
  }

  confirmDelete(): void {
    if (!this.deleteTarget || this.submittingDelete) return;
    const target = this.deleteTarget;
    this.submittingDelete = true;

    this.userService.delete(target.id).subscribe({
      next: () => {
        this.submittingDelete = false;
        this.deleteTarget = null;
        this.toast.success(`تم حذف المستخدم (${target.name}).`);
        this.loadUsers();
      },
      error: (err) => {
        this.submittingDelete = false;
        this.deleteTarget = null;
        this.toast.error(err.error?.message || 'تعذر حذف المستخدم.');
      }
    });
  }

  /** Reads the first message from an ASP.NET ModelState (400) response. */
  private firstValidationError(err: any): string | null {
    const errors = err?.error?.errors;
    if (!errors) return null;
    const first = Object.values(errors)[0] as string[] | undefined;
    return first?.[0] ?? null;
  }
}
