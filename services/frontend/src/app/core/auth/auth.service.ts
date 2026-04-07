import { Injectable, computed, signal } from '@angular/core'
import { decodeJwtPayload } from './jwt'
import {
  exchangeCodeForTokens,
  logoutRedirectUrl,
  redirectToKeycloakLogin,
  refreshAccessToken,
} from './oidc-http'
import { clearTokens, loadTokens, saveTokens } from './token-storage'
import type { AuthState, AuthUser, TokenBundle } from './auth.types'
import { realmRolesFromAccessToken } from './auth.types'

function bundleToUser(tokens: TokenBundle): AuthUser {
  const payload = decodeJwtPayload(tokens.accessToken)
  const sub = payload['sub']
  return {
    subject: typeof sub === 'string' ? sub : '',
    preferredUsername:
      typeof payload['preferred_username'] === 'string'
        ? payload['preferred_username']
        : undefined,
    name: typeof payload['name'] === 'string' ? payload['name'] : undefined,
    email: typeof payload['email'] === 'string' ? payload['email'] : undefined,
    realmRoles: realmRolesFromAccessToken(payload),
  }
}

function isExpired(tokens: TokenBundle, skewSec: number): boolean {
  const now = Math.floor(Date.now() / 1000)
  return now >= tokens.expiresAtEpochSec - skewSec
}

/** Evita troca duplicada do mesmo `code` (reexecução / Strict Mode). */
let lastExchangedCode: string | null = null

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _state = signal<AuthState>({ kind: 'loading' })

  readonly state = this._state.asReadonly()
  readonly isAuthenticated = computed(
    () => this._state().kind === 'authenticated',
  )

  getAccessToken(): string | null {
    const s = this._state()
    if (s.kind !== 'authenticated') return null
    return s.tokens.accessToken
  }

  async initialize(): Promise<void> {
    const tokens = loadTokens()
    if (!tokens) {
      this._state.set({ kind: 'unauthenticated' })
      return
    }

    if (!isExpired(tokens, 30)) {
      this._state.set({
        kind: 'authenticated',
        tokens,
        user: bundleToUser(tokens),
      })
      return
    }

    if (!tokens.refreshToken) {
      clearTokens()
      this._state.set({ kind: 'unauthenticated' })
      return
    }

    try {
      const refreshed = await refreshAccessToken(tokens.refreshToken)
      saveTokens(refreshed)
      this._state.set({
        kind: 'authenticated',
        tokens: refreshed,
        user: bundleToUser(refreshed),
      })
    } catch {
      clearTokens()
      this._state.set({ kind: 'unauthenticated' })
    }
  }

  async login(): Promise<void> {
    await redirectToKeycloakLogin()
  }

  logout(): void {
    const tokens = loadTokens()
    clearTokens()
    this._state.set({ kind: 'unauthenticated' })
    window.location.assign(logoutRedirectUrl(tokens?.idToken))
  }

  async completeLoginWithCode(code: string, codeVerifier: string): Promise<void> {
    if (lastExchangedCode === code) {
      return
    }
    lastExchangedCode = code
    try {
      const bundle = await exchangeCodeForTokens(code, codeVerifier)
      saveTokens(bundle)
      this._state.set({
        kind: 'authenticated',
        tokens: bundle,
        user: bundleToUser(bundle),
      })
    } catch (e) {
      lastExchangedCode = null
      throw e
    }
  }

  resetLastExchangeAttempt(): void {
    lastExchangedCode = null
  }
}
