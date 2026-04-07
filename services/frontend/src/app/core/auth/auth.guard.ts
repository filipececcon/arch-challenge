import { inject } from '@angular/core'
import { Router, type CanActivateFn } from '@angular/router'
import { AuthService } from './auth.service'

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService)
  const router = inject(Router)
  const s = auth.state()
  if (s.kind === 'authenticated') {
    return true
  }
  return router.createUrlTree(['/login'])
}

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService)
  const router = inject(Router)
  if (auth.isAuthenticated()) {
    return router.createUrlTree(['/'])
  }
  return true
}
