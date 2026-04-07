import { oidcConfig } from './oidc-config'

/** OIDC: autorização — GET com query (Authorization Code + PKCE). */
export function getAuthorizationEndpoint(): string {
  return `${oidcConfig.keycloakBaseUrl}/realms/${oidcConfig.realm}/protocol/openid-connect/auth`
}

/** OIDC: token — POST `application/x-www-form-urlencoded`. */
export function getTokenEndpoint(): string {
  return `${oidcConfig.keycloakBaseUrl}/realms/${oidcConfig.realm}/protocol/openid-connect/token`
}

/** OIDC: logout no IdP. */
export function getLogoutEndpoint(): string {
  return `${oidcConfig.keycloakBaseUrl}/realms/${oidcConfig.realm}/protocol/openid-connect/logout`
}
