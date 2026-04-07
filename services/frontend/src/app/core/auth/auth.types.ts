export type TokenBundle = {
  accessToken: string
  refreshToken?: string
  idToken?: string
  tokenType: string
  expiresAtEpochSec: number
}

export type RealmAccess = {
  roles?: string[]
}

export function realmRolesFromAccessToken(
  payload: Record<string, unknown>,
): string[] {
  const ra = payload['realm_access'] as RealmAccess | undefined
  return ra?.roles ?? []
}

export type AuthUser = {
  subject: string
  preferredUsername?: string
  name?: string
  email?: string
  realmRoles: string[]
}

export type AuthState =
  | { kind: 'loading' }
  | { kind: 'unauthenticated' }
  | { kind: 'authenticated'; tokens: TokenBundle; user: AuthUser }
