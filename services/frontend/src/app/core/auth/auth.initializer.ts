import type { AuthService } from './auth.service'

export function authAppInitializer(auth: AuthService): () => Promise<void> {
  return () => auth.initialize()
}
