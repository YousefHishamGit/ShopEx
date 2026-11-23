import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LicenseInfo } from '../../../models/models';
import { LicenseService } from '../../../services/license.service';
import { ToastService } from '../../../services/toast.service';

/** A change waiting for the developer's explicit confirmation (a mis-click here affects every user). */
type PendingAction = { kind: 'toggle'; enabled: boolean } | { kind: 'extend'; months: number };

@Component({
  selector: 'app-license',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './license.component.html'
})
export class LicenseComponent implements OnInit {
  license: LicenseInfo | null = null;
  loading = true;
  saving = false;
  loadError = '';
  pending: PendingAction | null = null;

  constructor(private licenses: LicenseService, private toast: ToastService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
    this.licenses.get().subscribe({
      next: (l) => {
        this.license = l;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.loadError = err.error?.message || 'تعذر تحميل بيانات الاشتراك.';
      }
    });
  }

  askToggle(): void {
    if (!this.license || this.saving) return;
    this.pending = { kind: 'toggle', enabled: !this.license.isEnabled };
  }

  askExtend(months: number): void {
    if (this.saving) return;
    this.pending = { kind: 'extend', months };
  }

  cancel(): void {
    this.pending = null;
  }

  confirm(): void {
    const action = this.pending;
    if (!action || this.saving) return;

    this.saving = true;
    const request$ =
      action.kind === 'toggle' ? this.licenses.setEnabled(action.enabled) : this.licenses.extend(action.months);

    request$.subscribe({
      next: (l) => {
        this.license = l;
        this.saving = false;
        this.pending = null;
        this.toast.success(
          action.kind === 'toggle'
            ? action.enabled ? 'تم تشغيل النظام للعميل.' : 'تم إيقاف النظام للعميل.'
            : `تم تمديد الاشتراك ${this.monthsLabel(action.months)}.`
        );
      },
      error: (err) => {
        this.saving = false;
        this.pending = null;
        this.toast.error(err.error?.message || 'تعذر تنفيذ العملية، حاول مرة أخرى.');
      }
    });
  }

  monthsLabel(months: number): string {
    if (months === 12) return 'سنة';
    if (months === 6) return '6 شهور';
    return `${months} شهر`;
  }

  pendingText(): string {
    const p = this.pending;
    if (!p) return '';
    if (p.kind === 'extend') return `تمديد الاشتراك ${this.monthsLabel(p.months)}؟`;
    return p.enabled ? 'تشغيل النظام للعميل؟' : 'إيقاف النظام؟ كل المستخدمين (الأدمن والموظفين) هيتمنعوا فوراً.';
  }

  statusLabel(): string {
    switch (this.license?.status) {
      case 'Active': return 'فعّال';
      case 'Disabled': return 'متوقف';
      case 'Expired': return 'منتهي';
      default: return '';
    }
  }
}
