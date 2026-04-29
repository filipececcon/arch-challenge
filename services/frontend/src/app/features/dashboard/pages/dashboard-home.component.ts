import { Component } from '@angular/core'
import { StatCardComponent } from '../components/stat-card.component'
import { TransactionChartComponent } from '../components/transaction-chart.component'
import { RecentTransactionsComponent } from '../components/recent-transactions.component'
import { CreditCardWidgetComponent } from '../components/credit-card-widget.component'
import { QuickActionsComponent } from '../components/quick-actions.component'
import { ExpensesChartComponent } from '../components/expenses-chart.component'

@Component({
  selector: 'app-dashboard-home',
  standalone: true,
  imports: [
    StatCardComponent,
    TransactionChartComponent,
    RecentTransactionsComponent,
    CreditCardWidgetComponent,
    QuickActionsComponent,
    ExpensesChartComponent,
  ],
  template: `
    <div class="flex h-full flex-col overflow-hidden">

      <!-- Top bar -->
      <div class="flex flex-shrink-0 items-center justify-between border-b border-navy-800 px-8 py-4">
        <h1 class="text-2xl font-bold text-white">Home</h1>
        <div class="flex items-center gap-4">
          <!-- Search -->
          <div class="flex w-64 items-center gap-2.5 rounded-xl bg-navy-800 px-4 py-2.5">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 flex-shrink-0 text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" />
            </svg>
            <input
              class="w-full bg-transparent text-sm text-slate-300 placeholder-slate-500 outline-none"
              placeholder="Search here"
              type="search"
            />
          </div>
          <!-- User -->
          <div class="flex items-center gap-3">
            <div class="flex h-9 w-9 items-center justify-center rounded-full bg-gradient-to-br from-brand-light to-brand text-sm font-bold text-white">
              M
            </div>
            <div>
              <p class="text-sm font-semibold text-white">Marvin McKinney</p>
              <p class="text-xs text-slate-500">&#64;codyfisher</p>
            </div>
            <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
            </svg>
          </div>
        </div>
      </div>

      <!-- Content -->
      <div class="flex flex-1 gap-6 overflow-hidden px-8 py-6">

        <!-- Left column -->
        <div class="flex flex-1 flex-col gap-5 overflow-y-auto pr-1">
          <!-- Stat cards -->
          <div class="grid grid-cols-3 gap-4">
            <app-stat-card
              title="Total Balance"
              value="$10,250.00"
              change="+2.1%"
              changeLabel="Higher than Last Month"
              [positive]="true"
            />
            <app-stat-card
              title="Debit"
              value="$3,500.00"
              change="-2.1%"
              changeLabel="Higher than Last Month"
              [positive]="false"
            />
            <app-stat-card
              title="Credit"
              value="$4,200.00"
              change="+3.8%"
              changeLabel="Increase in Deposits"
              [positive]="true"
            />
          </div>

          <!-- Transaction chart -->
          <app-transaction-chart />

          <!-- Recent transactions -->
          <app-recent-transactions />
        </div>

        <!-- Right column -->
        <div class="flex w-[300px] flex-shrink-0 flex-col gap-5 overflow-y-auto">
          <app-credit-card-widget />
          <app-quick-actions />
          <app-expenses-chart />
        </div>

      </div>
    </div>
  `,
})
export class DashboardHomeComponent {}
