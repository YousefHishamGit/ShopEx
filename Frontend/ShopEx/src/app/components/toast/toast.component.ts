import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, ToastMessage } from '../../services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="fixed top-5 left-5 z-50 flex flex-col gap-3 max-w-md w-full pointer-events-none" dir="rtl">
      <div
        *ngFor="let toast of toastService.toasts$ | async"
        class="pointer-events-auto transform transition-all duration-300 ease-out shadow-xl rounded-2xl p-4 flex items-start gap-3 border backdrop-blur-md"
        [ngClass]="{
          'bg-emerald-50/95 border-emerald-300 text-emerald-900': toast.type === 'success',
          'bg-rose-50/95 border-rose-300 text-rose-900': toast.type === 'error',
          'bg-amber-50/95 border-amber-300 text-amber-900': toast.type === 'warning',
          'bg-blue-50/95 border-blue-300 text-blue-900': toast.type === 'info'
        }"
      >
        <!-- Icon -->
        <div class="w-9 h-9 rounded-xl flex items-center justify-center shrink-0"
          [ngClass]="{
            'bg-emerald-600 text-white': toast.type === 'success',
            'bg-rose-600 text-white': toast.type === 'error',
            'bg-amber-500 text-white': toast.type === 'warning',
            'bg-blue-600 text-white': toast.type === 'info'
          }">
          <span class="material-symbols-outlined text-[20px]">
            {{ toast.type === 'success' ? 'check_circle' : toast.type === 'error' ? 'error' : toast.type === 'warning' ? 'warning' : 'info' }}
          </span>
        </div>

        <!-- Content -->
        <div class="flex-1 min-w-0 pt-0.5">
          <div *ngIf="toast.title" class="font-bold text-sm mb-0.5">
            {{ toast.title }}
          </div>
          <div class="text-xs leading-relaxed font-medium opacity-90 break-words">
            {{ toast.message }}
          </div>
        </div>

        <!-- Close Button -->
        <button
          type="button"
          (click)="toastService.dismiss(toast.id)"
          class="shrink-0 text-slate-400 hover:text-slate-700 p-1 rounded-lg transition-colors"
          aria-label="إغلاق التنبيه"
        >
          <span class="material-symbols-outlined text-[18px]">close</span>
        </button>
      </div>
    </div>
  `
})
export class ToastContainerComponent {
  constructor(public toastService: ToastService) {}
}
