import { environment } from '../../../environments/environment'

export const oidcConfig = {
  keycloakBaseUrl: environment.keycloakUrl.replace(/\/$/, ''),
  realm: environment.realm,
  clientId: environment.clientId,
  appOrigin: environment.appOrigin.replace(/\/$/, ''),
  gatewayUrl: environment.gatewayUrl.replace(/\/$/, ''),
} as const

export function getRedirectUri(): string {
  return `${oidcConfig.appOrigin}/auth/callback`
}
