import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface ToastMessage {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title?: string;
  message: string;
  duration?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toastsSubject = new BehaviorSubject<ToastMessage[]>([]);
  public toasts$: Observable<ToastMessage[]> = this.toastsSubject.asObservable();

  show(message: string, type: 'success' | 'error' | 'warning' | 'info' = 'info', title?: string, duration: number = 4000): void {
    const id = Date.now().toString(36) + Math.random().toString(36).substring(2, 5);
    const toast: ToastMessage = { id, type, title, message, duration };

    const current = this.toastsSubject.value;
    this.toastsSubject.next([...current, toast]);

    if (duration > 0) {
      setTimeout(() => {
        this.dismiss(id);
      }, duration);
    }
  }

  success(message: string, title: string = 'تمت العملية بنجاح'): void {
    this.show(message, 'success', title, 4000);
  }

  error(message: string, title: string = 'حدث خطأ'): void {
    this.show(message, 'error', title, 6000);
  }

  warning(message: string, title: string = 'تنبيه'): void {
    this.show(message, 'warning', title, 5000);
  }

  info(message: string, title: string = 'معلومة'): void {
    this.show(message, 'info', title, 4000);
  }

  dismiss(id: string): void {
    const updated = this.toastsSubject.value.filter(t => t.id !== id);
    this.toastsSubject.next(updated);
  }

  clearAll(): void {
    this.toastsSubject.next([]);
  }
}
