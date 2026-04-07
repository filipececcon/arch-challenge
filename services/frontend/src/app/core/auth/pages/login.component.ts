import { Component, inject } from '@angular/core'
import { Router } from '@angular/router'
import { AuthService } from '../auth.service'

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [],
  template: `
    <div
      class="relative flex min-h-dvh flex-col items-center justify-center overflow-hidden px-4"
    >
        <div
          class="pointer-events-none absolute inset-0 opacity-40"
          style="background: radial-gradient(ellipse 80% 50% at 50% -20%, rgb(56 189 248 / 0.35), transparent);"
        ></div>
        <div
          class="relative w-full max-w-md rounded-2xl border border-slate-800/80 bg-slate-900/60 p-8 shadow-2xl shadow-sky-950/50 backdrop-blur-md"
        >
          <div class="mb-8 text-center">
            <h1 class="text-2xl font-semibold tracking-tight text-white">
              Arch Challenge
            </h1>
            <p class="mt-2 text-sm text-slate-400">
              Portal da solução — CashFlow e Dashboard
            </p>
          </div>
          <p class="mb-6 text-center text-sm leading-relaxed text-slate-400">
            Autenticação com Keycloak — Authorization Code + PKCE, conforme a
            documentação do projeto.
          </p>
          <button
            type="button"
            (click)="onLogin()"
            class="w-full rounded-xl bg-sky-500 px-4 py-3 text-sm font-medium text-slate-950 transition hover:bg-sky-400 focus:outline-none focus:ring-2 focus:ring-sky-400/50"
          >
            Entrar com Keycloak
          </button>
          <p class="mt-6 text-center text-xs text-slate-500">
            Você será redirecionado para o provedor de identidade.
          </p>
        </div>
      </div>
  `,
})
export class LoginComponent {
  readonly auth = inject(AuthService)
  private readonly router = inject(Router)

  constructor() {
    if (this.auth.isAuthenticated()) {
      void this.router.navigateByUrl('/')
    }
  }

  onLogin(): void {
    void this.auth.login()
  }
}
