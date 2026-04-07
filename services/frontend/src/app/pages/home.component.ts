import { Component } from '@angular/core'
import { RouterLink } from '@angular/router'

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="space-y-8">
      <div>
        <h1 class="text-2xl font-semibold tracking-tight text-white">Bem-vindo</h1>
        <p class="mt-2 max-w-2xl text-sm leading-relaxed text-slate-400">
          Esta é a SPA unificada da solução: os domínios
          <span class="text-slate-300">CashFlow</span> e
          <span class="text-slate-300">Dashboard</span> convivem no mesmo projeto
          Angular, com rotas e pastas separadas por feature.
        </p>
      </div>
      <ul class="grid gap-4 sm:grid-cols-2">
        <li>
          <a
            routerLink="/cashflow"
            class="block rounded-xl border border-slate-800 bg-slate-900/40 p-5 transition hover:border-sky-900/80 hover:bg-slate-900/80"
          >
            <h2 class="font-medium text-sky-300">CashFlow</h2>
            <p class="mt-2 text-sm text-slate-500">
              Lançamentos e transações (roles: comerciante, admin).
            </p>
          </a>
        </li>
        <li>
          <a
            routerLink="/dashboard"
            class="block rounded-xl border border-slate-800 bg-slate-900/40 p-5 transition hover:border-sky-900/80 hover:bg-slate-900/80"
          >
            <h2 class="font-medium text-sky-300">Dashboard</h2>
            <p class="mt-2 text-sm text-slate-500">
              Consolidados e saldos (roles: gestor, admin).
            </p>
          </a>
        </li>
      </ul>
    </div>
  `,
})
export class HomeComponent {}
