import { Component } from '@angular/core'

@Component({
  selector: 'app-dashboard-home',
  standalone: true,
  template: `
    <div>
      <h1 class="text-xl font-semibold text-white">Dashboard</h1>
      <p class="mt-2 text-sm text-slate-400">
        Área do domínio de consolidados. Exemplo de rota no gateway:
        <code class="rounded bg-slate-800 px-1.5 py-0.5 text-xs text-sky-300">
          GET /dashboard/v1/consolidate
        </code>
        .
      </p>
    </div>
  `,
})
export class DashboardHomeComponent {}
