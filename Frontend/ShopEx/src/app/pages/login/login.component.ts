import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  username = '';
  password = '';
  showPassword = false;
  submitting = false;
  errorMessage = '';

  constructor(
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  onSubmit(): void {
    if (this.submitting) return;

    if (!this.username.trim() || !this.password) {
      this.errorMessage = 'يرجى إدخال اسم المستخدم وكلمة المرور.';
      return;
    }

    this.submitting = true;
    this.errorMessage = '';

    this.auth.login({ username: this.username, password: this.password }).subscribe({
      next: () => {
        this.submitting = false;
        this.password = '';
        this.router.navigateByUrl(this.safeReturnUrl() ?? this.auth.homeRoute());
      },
      error: (err) => {
        this.submitting = false;
        this.password = '';
        this.errorMessage =
          err.status === 0
            ? 'تعذر الاتصال بالخادم. تأكد من تشغيل الـ API.'
            : err.error?.message || 'تعذر تسجيل الدخول، حاول مرة أخرى.';
      }
    });
  }

  /** Only same-app absolute paths are accepted (prevents open redirects). */
  private safeReturnUrl(): string | null {
    const url = this.route.snapshot.queryParamMap.get('returnUrl');
    return url && url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/login') ? url : null;
  }
}
