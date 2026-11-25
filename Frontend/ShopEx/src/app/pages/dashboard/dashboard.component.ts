import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardData, DashboardPnlRow } from '../../models/models';

/** Local calendar day as yyyy-MM-dd (toISOString would shift the day by the UTC offset). */
function toDateInput(d: Date): string {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit, OnDestroy {
  /** Refresh interval while the dashboard shows today. */
  private readonly autoRefreshMs = 60_000;

  data: DashboardData | null = null;
  loading = false;
  errorMessage = '';

  selectedDate = toDateInput(new Date());

  private loadSub?: Subscription;
  private timer?: ReturnType<typeof setInterval>;

  constructor(private dashboardService: DashboardService) {}

  get isToday(): boolean {
    return this.selectedDate === toDateInput(new Date());
  }

  ngOnInit(): void {
    this.load();
    this.timer = setInterval(() => {
      // Only keep refreshing a live day, and not while the tab is in the background.
      if (this.isToday && !document.hidden) this.load(true);
    }, this.autoRefreshMs);
  }

  ngOnDestroy(): void {
    this.loadSub?.unsubscribe();
    if (this.timer) clearInterval(this.timer);
  }

  /** @param silent keep the current numbers on screen (no spinner / no error banner) — used by auto-refresh. */
  load(silent = false): void {
    // A slow older response must never overwrite a newer one.
    this.loadSub?.unsubscribe();
    if (!silent) {
      this.loading = true;
      this.errorMessage = '';
    }

    this.loadSub = this.dashboardService.get(this.selectedDate || undefined).subscribe({
      next: (res) => {
        this.data = res;
        this.loading = false;
        this.errorMessage = '';
      },
      error: (err) => {
        this.loading = false;
        if (!silent) {
          this.errorMessage = err?.error?.message || 'تعذر تحميل بيانات اللوحة. تأكد من اتصال الـ API وقاعدة البيانات.';
        }
      }
    });
  }

  onDateChange(): void {
    if (!this.selectedDate) this.selectedDate = toDateInput(new Date());
    this.load();
  }

  goToday(): void {
    this.selectedDate = toDateInput(new Date());
    this.load();
  }

  shiftDay(delta: number): void {
    const [y, m, d] = this.selectedDate.split('-').map(Number);
    this.selectedDate = toDateInput(new Date(y, m - 1, d + delta));
    this.load();
  }

  /** Human text for how far a due date is: "متأخرة 3 يوم" / "مستحقة اليوم" / "بعد 2 يوم". */
  dueLabel(days: number): string {
    if (days < 0) return `متأخرة ${Math.abs(days)} يوم`;
    if (days === 0) return 'مستحقة اليوم';
    return `بعد ${days} يوم`;
  }

  dueClass(days: number): string {
    if (days < 0) return 'bg-red-50 text-red-700 border-red-200';
    if (days === 0) return 'bg-amber-50 text-amber-700 border-amber-200';
    return 'bg-blue-50 text-blue-700 border-blue-200';
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
}
