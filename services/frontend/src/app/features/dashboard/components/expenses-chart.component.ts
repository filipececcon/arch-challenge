import { Component } from '@angular/core'

interface ExpenseCategory {
  label: string
  pct: number
  color: string
  dasharray: string
  rotation: number
}

@Component({
  selector: 'app-expenses-chart',
  standalone: true,
  template: `
    <div class="rounded-2xl border border-navy-700/40 bg-navy-800 p-5">
      <!-- Header -->
      <div class="mb-5 flex items-center justify-between">
        <h3 class="text-base font-semibold text-white">Expenses</h3>
        <button class="flex items-center gap-1.5 rounded-lg bg-navy-700/60 px-3 py-1.5 text-xs font-medium text-slate-300 transition-colors hover:bg-navy-600">
          Monthly
          <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
          </svg>
        </button>
      </div>

      <!-- Donut chart -->
      <div class="relative mx-auto mb-5 h-44 w-44">
        <svg viewBox="0 0 180 180" class="h-full w-full -rotate-90">
          <!-- Track -->
          <circle cx="90" cy="90" r="60" fill="none" stroke="#152c44" stroke-width="26"/>
          <!-- Segments (r=60, circumference≈376.99) -->
          <!-- Shopping 30% -->
          <circle cx="90" cy="90" r="60" fill="none"
            stroke="#06b6d4" stroke-width="26"
            stroke-dasharray="109 268"
            transform="rotate(0 90 90)"
          />
          <!-- Food 20% -->
          <circle cx="90" cy="90" r="60" fill="none"
            stroke="#4361ee" stroke-width="26"
            stroke-dasharray="71 306"
            transform="rotate(108 90 90)"
          />
          <!-- Travel 35% -->
          <circle cx="90" cy="90" r="60" fill="none"
            stroke="#8b5cf6" stroke-width="26"
            stroke-dasharray="128 249"
            transform="rotate(180 90 90)"
          />
          <!-- Health 15% -->
          <circle cx="90" cy="90" r="60" fill="none"
            stroke="#ec4899" stroke-width="26"
            stroke-dasharray="53 324"
            transform="rotate(306 90 90)"
          />
        </svg>
        <!-- Center label -->
        <div class="absolute inset-0 flex flex-col items-center justify-center">
          <span class="text-xs text-slate-400">Total</span>
          <span class="text-xl font-bold text-white">$1500</span>
        </div>
      </div>

      <!-- Legend -->
      <div class="grid grid-cols-2 gap-x-4 gap-y-2">
        @for (cat of categories; track cat.label) {
          <div class="flex items-center gap-2">
            <span class="h-2 w-2 flex-shrink-0 rounded-full" [style.background-color]="cat.color"></span>
            <span class="text-xs text-slate-400">{{ cat.label }}</span>
            <span class="ml-auto text-xs font-semibold text-slate-300">{{ cat.pct }}%</span>
          </div>
        }
      </div>
    </div>
  `,
})
export class ExpensesChartComponent {
  readonly categories: ExpenseCategory[] = [
    { label: 'Shopping', pct: 30, color: '#06b6d4', dasharray: '109 268', rotation: 0 },
    { label: 'Food',     pct: 20, color: '#4361ee', dasharray: '71 306',  rotation: 108 },
    { label: 'Travel',   pct: 35, color: '#8b5cf6', dasharray: '128 249', rotation: 180 },
    { label: 'Health',   pct: 15, color: '#ec4899', dasharray: '53 324',  rotation: 306 },
  ]
}
