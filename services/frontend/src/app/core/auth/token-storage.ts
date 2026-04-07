import type { TokenBundle } from './auth.types'

const KEY = 'arch_challenge_oidc'

export function loadTokens(): TokenBundle | null {
  try {
    const raw = sessionStorage.getItem(KEY)
    if (!raw) return null
    return JSON.parse(raw) as TokenBundle
  } catch {
    return null
  }
}

export function saveTokens(bundle: TokenBundle): void {
  sessionStorage.setItem(KEY, JSON.stringify(bundle))
}

export function clearTokens(): void {
  sessionStorage.removeItem(KEY)
}
