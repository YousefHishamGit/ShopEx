import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
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

  /** True on the hidden /dev/login route: signs in against the developer table instead. */
  readonly devMode: boolean;

  constructor(
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.devMode = !!route.snapshot.data['dev'];
  }

  onSubmit(): void {
    if (this.submitting) return;

    if (!this.username.trim() || !this.password) {
      this.errorMessage = 'يرجى إدخال اسم المستخدم وكلمة المرور.';
      return;
    }

    this.submitting = true;
    this.errorMessage = '';

    const request = { username: this.username, password: this.password };
    const login$: Observable<unknown> = this.devMode ? this.auth.loginDev(request) : this.auth.login(request);

    login$.subscribe({
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
    return url && url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/login') && !url.startsWith('/dev/login')
      ? url
      : null;
  }
}
