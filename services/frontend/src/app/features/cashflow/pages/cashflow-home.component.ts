import { Component } from '@angular/core'

@Component({
  selector: 'app-cashflow-home',
  standalone: true,
  template: `
    <div>
      <h1 class="text-xl font-semibold text-white">CashFlow</h1>
      <p class="mt-2 text-sm text-slate-400">
        Área do domínio de lançamentos. As chamadas à API devem usar o gateway com o
        token Bearer — por exemplo
        <code class="rounded bg-slate-800 px-1.5 py-0.5 text-xs text-sky-300">
          GET /cashflow/v1/transaction
        </code>
        .
      </p>
    </div>
  `,
})
export class CashflowHomeComponent {}
