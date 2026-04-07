import { getRedirectUri, oidcConfig } from './oidc-config'
import {
  getAuthorizationEndpoint,
  getLogoutEndpoint,
  getTokenEndpoint,
} from './oidc-endpoints'
import { createOidcState, createPkcePair } from './pkce'
import type { TokenBundle } from './auth.types'
import { savePkceSession } from './transient-oidc'

type RawTokenResponse = {
  access_token: string
  expires_in: number
  refresh_token?: string
  id_token?: string
  token_type: string
}

function toBundle(raw: RawTokenResponse): TokenBundle {
  const now = Math.floor(Date.now() / 1000)
  return {
    accessToken: raw.access_token,
    refreshToken: raw.refresh_token,
    idToken: raw.id_token,
    tokenType: raw.token_type,
    expiresAtEpochSec: now + raw.expires_in,
  }
}

export async function redirectToKeycloakLogin(): Promise<void> {
  const { codeVerifier, codeChallenge } = await createPkcePair()
  const state = createOidcState()
  savePkceSession(codeVerifier, state)

  const params = new URLSearchParams({
    client_id: oidcConfig.clientId,
    redirect_uri: getRedirectUri(),
    response_type: 'code',
    scope: 'openid profile email',
    state,
    code_challenge: codeChallenge,
    code_challenge_method: 'S256',
  })

  const url = `${getAuthorizationEndpoint()}?${params.toString()}`
  window.location.assign(url)
}

export async function exchangeCodeForTokens(
  code: string,
  codeVerifier: string,
): Promise<TokenBundle> {
  const body = new URLSearchParams({
    grant_type: 'authorization_code',
    client_id: oidcConfig.clientId,
    code,
    redirect_uri: getRedirectUri(),
    code_verifier: codeVerifier,
  })

  const res = await fetch(getTokenEndpoint(), {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body,
  })

  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || `Token endpoint: ${res.status}`)
  }

  const raw = (await res.json()) as RawTokenResponse
  return toBundle(raw)
}

export async function refreshAccessToken(
  refreshToken: string,
): Promise<TokenBundle> {
  const body = new URLSearchParams({
    grant_type: 'refresh_token',
    client_id: oidcConfig.clientId,
    refresh_token: refreshToken,
  })

  const res = await fetch(getTokenEndpoint(), {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body,
  })

  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || `Refresh: ${res.status}`)
  }

  const raw = (await res.json()) as RawTokenResponse
  return toBundle(raw)
}

export function logoutRedirectUrl(idToken?: string): string {
  const params = new URLSearchParams()
  if (idToken) {
    params.set('id_token_hint', idToken)
  }
  params.set('post_logout_redirect_uri', `${oidcConfig.appOrigin}/login`)
  return `${getLogoutEndpoint()}?${params.toString()}`
}
