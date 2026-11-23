import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/** HQ pages: Admin only. Employees are sent to their own area, guests to the login page. */
export const adminGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }
  return auth.isAdmin() ? true : router.createUrlTree([auth.homeRoute()]);
};

/** Branch pages: Employee only. */
export const employeeGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }
  return auth.isEmployee() ? true : router.createUrlTree([auth.homeRoute()]);
};

/** Developer area (license control): Dev only. Everyone else is sent to their own home. */
export const devGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) {
    return router.createUrlTree(['/dev/login'], { queryParams: { returnUrl: state.url } });
  }
  return auth.isDev() ? true : router.createUrlTree([auth.homeRoute()]);
};

/** Login page: signed-in users go straight to their home page. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() ? router.createUrlTree([auth.homeRoute()]) : true;
};
