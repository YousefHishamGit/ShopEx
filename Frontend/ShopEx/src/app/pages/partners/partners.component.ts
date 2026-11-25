import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { PartnerService } from '../../services/partner.service';
import { ToastService } from '../../services/toast.service';
import { BranchProfitShare, DashboardPnlRow, PartnersReport } from '../../models/models';

/** Local calendar day as yyyy-MM-dd (toISOString would shift the day by the UTC offset). */
function toDateInput(d: Date): string {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

type Preset = 'today' | 'month' | 'lastMonth' | 'year';

interface PartnerFormRow {
  name: string;
  percent: number | null;
}

@Component({
  selector: 'app-partners',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './partners.component.html'
})
export class PartnersComponent implements OnInit, OnDestroy {
  data: PartnersReport | null = null;
  loading = false;
  errorMessage = '';

  fromDate = '';
  toDate = '';
  preset: Preset | null = 'month';

  readonly presets: { value: Preset; label: string }[] = [
    { value: 'today', label: 'اليوم' },
    { value: 'month', label: 'هذا الشهر' },
    { value: 'lastMonth', label: 'الشهر الماضي' },
    { value: 'year', label: 'هذه السنة' }
  ];

  // ----- Edit partners modal -----
  editing: BranchProfitShare | null = null;
  zakatPercent: number | null = 2.5;
  rows: PartnerFormRow[] = [];
  saving = false;
  formError = '';

  private loadSub?: Subscription;

  constructor(
    private partnerService: PartnerService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.applyPreset('month');
  }

  ngOnDestroy(): void {
    this.loadSub?.unsubscribe();
  }

  // ========== Filter ==========

  applyPreset(p: Preset): void {
    const now = new Date();
    const y = now.getFullYear();
    const m = now.getMonth();
    this.preset = p;
    switch (p) {
      case 'today':
        this.fromDate = this.toDate = toDateInput(now);
        break;
      case 'month':
        this.fromDate = toDateInput(new Date(y, m, 1));
        this.toDate = toDateInput(now);
        break;
      case 'lastMonth':
        this.fromDate = toDateInput(new Date(y, m - 1, 1));
        this.toDate = toDateInput(new Date(y, m, 0));
        break;
      case 'year':
        this.fromDate = toDateInput(new Date(y, 0, 1));
        this.toDate = toDateInput(now);
        break;
    }
    this.load();
  }

  onDatesChange(): void {
    this.preset = null;
    this.load();
  }

  load(): void {
    if (!this.fromDate || !this.toDate) return;
    if (this.fromDate > this.toDate) {
      this.errorMessage = 'تاريخ "من" يجب أن يكون قبل أو يساوي تاريخ "إلى".';
      return;
    }

    // A slow older response must never overwrite a newer one.
    this.loadSub?.unsubscribe();
    this.loading = true;
    this.errorMessage = '';

    this.loadSub = this.partnerService.getReport(this.fromDate, this.toDate).subscribe({
      next: (res) => {
        this.data = res;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err?.error?.message || 'تعذر تحميل تقرير الشركاء. تأكد من اتصال الـ API وقاعدة البيانات.';
      }
    });
  }

  // ========== Edit partners ==========

  openEdit(branch: BranchProfitShare): void {
    this.editing = branch;
    this.zakatPercent = branch.zakatPercent;
    this.rows = branch.partners.map((p) => ({ name: p.name, percent: p.percent }));
    if (this.rows.length === 0) this.addRow();
    this.formError = '';
  }

  closeEdit(): void {
    if (this.saving) return;
    this.editing = null;
  }

  addRow(): void {
    this.rows.push({ name: '', percent: null });
  }

  removeRow(i: number): void {
    this.rows.splice(i, 1);
  }

  get formTotal(): number {
    return this.rows.reduce((s, r) => s + (Number(r.percent) || 0), 0);
  }

  /** Floating point safe: 33.33 * 3 etc. */
  get formOver(): boolean {
    return Math.round(this.formTotal * 100) > 10000;
  }

  get formRemaining(): number {
    return Math.round((100 - this.formTotal) * 100) / 100;
  }

  save(): void {
    if (!this.editing || this.saving) return;

    const zakat = Number(this.zakatPercent ?? 0);
    if (!(zakat >= 0 && zakat <= 100)) {
      this.formError = 'نسبة الزكاة يجب أن تكون بين 0 و 100.';
      return;
    }

    // Empty rows are just ignored; a row with a name needs a valid percent.
    const partners = this.rows
      .map((r) => ({ name: (r.name || '').trim(), percent: Number(r.percent) }))
      .filter((r) => r.name || r.percent);

    for (const p of partners) {
      if (!p.name) {
        this.formError = 'اسم الشريك مطلوب.';
        return;
      }
      if (!(p.percent > 0 && p.percent <= 100)) {
        this.formError = `نسبة الشريك "${p.name}" يجب أن تكون أكبر من 0 وحتى 100.`;
        return;
      }
    }
    if (this.formOver) {
      this.formError = 'مجموع النسب أكبر من 100%.';
      return;
    }

    this.saving = true;
    this.formError = '';
    this.partnerService.saveBranchPartners(this.editing.storeId, { zakatPercent: zakat, partners }).subscribe({
      next: () => {
        this.saving = false;
        this.editing = null;
        this.toast.success('تم حفظ شركاء الفرع');
        this.load();
      },
      error: (err) => {
        this.saving = false;
        this.formError = err?.error?.message || 'تعذر حفظ الشركاء.';
      }
    });
  }

  /** Open/close the HQ card (branches are always on). */
  togglingHq = false;

  toggleHq(enabled: boolean): void {
    if (this.togglingHq) return;
    this.togglingHq = true;
    this.partnerService.setHqEnabled(enabled).subscribe({
      next: () => {
        this.togglingHq = false;
        // Reflect it immediately, then refresh the numbers.
        const hq = this.data?.branches.find((b) => b.isHq);
        if (hq) hq.isEnabled = enabled;
        this.load();
      },
      error: (err) => {
        this.togglingHq = false;
        this.toast.error(err?.error?.message || 'تعذر تغيير حالة الإدارة العامة.');
      }
    });
  }

  // ========== Table helpers ==========

  trackBranch(_: number, b: BranchProfitShare): number {
    return b.storeId;
  }

  trackPnl(_: number, r: DashboardPnlRow): string {
    return r.kind + (r.storeId ?? '');
  }

  /** True for the first row of a new group (branches / HQ / maintenance / devices ...) to draw a separator. */
  isFirstOfOtherKind(rows: DashboardPnlRow[], r: DashboardPnlRow): boolean {
    const i = rows.indexOf(r);
    if (i <= 0) return false;
    const group = (k: string) => (k === 'branch' || k === 'hq' ? 'branch' : k);
    return group(rows[i - 1].kind) !== group(r.kind) && group(r.kind) === 'maintenance';
  }

  distributedAmount(b: BranchProfitShare): number {
    return b.partners.reduce((s, p) => s + p.amount, 0);
  }
}
