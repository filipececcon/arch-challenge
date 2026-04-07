import { Component, inject } from '@angular/core'
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router'
import { AuthService } from '../core/auth/auth.service'
import { oidcConfig } from '../core/auth/oidc-config'

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    @if (auth.state(); as s) {
      @if (s.kind === 'authenticated') {
        <div class="flex min-h-dvh flex-col">
          <header class="border-b border-slate-800/80 bg-slate-900/50 backdrop-blur-sm">
            <div
              class="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-4 px-4 py-4"
            >
              <div class="flex flex-wrap items-center gap-6">
                <span class="text-sm font-semibold tracking-tight text-white">
                  Arch Challenge
                </span>
                <nav class="flex flex-wrap gap-1">
                  <a
                    routerLink="/"
                    routerLinkActive="bg-slate-800 text-white"
                    [routerLinkActiveOptions]="{ exact: true }"
                    class="rounded-lg px-3 py-2 text-sm font-medium text-slate-400 transition hover:bg-slate-800/60 hover:text-slate-200"
                    >Início</a>
                  <a
                    routerLink="/cashflow"
                    routerLinkActive="bg-slate-800 text-white"
                    class="rounded-lg px-3 py-2 text-sm font-medium text-slate-400 transition hover:bg-slate-800/60 hover:text-slate-200"
                    >CashFlow</a>
                  <a
                    routerLink="/dashboard"
                    routerLinkActive="bg-slate-800 text-white"
                    class="rounded-lg px-3 py-2 text-sm font-medium text-slate-400 transition hover:bg-slate-800/60 hover:text-slate-200"
                    >Dashboard</a>
                </nav>
              </div>
              <div class="flex items-center gap-3 text-right">
                <div class="hidden text-xs text-slate-500 sm:block">
                  Gateway:
                  <span class="text-slate-400">{{ gatewayUrl }}</span>
                </div>
                <div class="text-right text-sm">
                  <div class="font-medium text-slate-200">
                    {{
                      s.user.preferredUsername ?? s.user.name ?? s.user.subject
                    }}
                  </div>
                  <div class="max-w-[12rem] truncate text-xs text-slate-500">
                    {{
                      s.user.realmRoles.length
                        ? s.user.realmRoles.join(', ')
                        : 'sem roles'
                    }}
                  </div>
                </div>
                <button
                  type="button"
                  (click)="auth.logout()"
                  class="rounded-lg border border-slate-700 px-3 py-1.5 text-xs font-medium text-slate-300 transition hover:border-slate-600 hover:bg-slate-800"
                >
                  Sair
                </button>
              </div>
            </div>
          </header>
          <main class="mx-auto w-full max-w-5xl flex-1 px-4 py-8">
            <router-outlet />
          </main>
        </div>
      }
    }
  `,
})
export class AppShellComponent {
  readonly auth = inject(AuthService)
  readonly gatewayUrl = oidcConfig.gatewayUrl
}
