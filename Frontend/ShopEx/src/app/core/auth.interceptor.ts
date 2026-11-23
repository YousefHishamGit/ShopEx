import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

/**
 * Attaches the JWT to API calls only (never to third-party URLs),
 * signs the user out when the server rejects the token, and bounces 403s home.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);

  const isApiCall = req.url.startsWith(AuthService.apiUrl);
  const isLogin =
    req.url === `${AuthService.apiUrl}/Auth/login` || req.url === `${AuthService.apiUrl}/Dev/login`;

  let outgoing = req;
  if (isApiCall && !isLogin) {
    const token = auth.getToken();
    if (token) {
      outgoing = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
    }
  }

  return next(outgoing).pipe(
    catchError((err: HttpErrorResponse) => {
      if (isApiCall && !isLogin) {
        if (err.status === 401) {
          // expired / revoked / disabled account
          const returnUrl = router.url.startsWith('/login') ? undefined : router.url;
          auth.logout(true, returnUrl);
          toast.warning('انتهت الجلسة، يرجى تسجيل الدخول مرة أخرى.');
        } else if (err.status === 403 && err.error?.code === 'LICENSE_EXPIRED') {
          // الاشتراك منتهي/متوقف: خروج فوري برسالة واضحة بدل "ليست لديك صلاحية"
          auth.logout();
          toast.error(err.error?.message || 'الاشتراك منتهي أو متوقف. تواصل مع المطوّر.', 'الاشتراك غير ساري');
        } else if (err.status === 403) {
          toast.error('ليست لديك صلاحية لتنفيذ هذا الإجراء.');
          router.navigateByUrl(auth.homeRoute());
        }
      }
      return throwError(() => err);
    })
  );
};
