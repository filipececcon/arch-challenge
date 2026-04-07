const PKCE_VERIFIER = 'arch_challenge_pkce_verifier'
const OIDC_STATE = 'arch_challenge_oidc_state'

export function savePkceSession(codeVerifier: string, state: string): void {
  sessionStorage.setItem(PKCE_VERIFIER, codeVerifier)
  sessionStorage.setItem(OIDC_STATE, state)
}

export function getPkceVerifierIfStateValid(stateFromUrl: string): string | null {
  const expected = sessionStorage.getItem(OIDC_STATE)
  const verifier = sessionStorage.getItem(PKCE_VERIFIER)
  if (!expected || !verifier || expected !== stateFromUrl) {
    return null
  }
  return verifier
}

export function clearPkceSession(): void {
  sessionStorage.removeItem(OIDC_STATE)
  sessionStorage.removeItem(PKCE_VERIFIER)
}
