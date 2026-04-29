import { Component } from '@angular/core'

interface BarData {
  month: string
  pct: number
  highlighted: boolean
}

@Component({
  selector: 'app-transaction-chart',
  standalone: true,
  imports: [],
  template: `
    <div class="rounded-2xl border border-navy-700/40 bg-navy-800 p-5">
      <!-- Header -->
      <div class="mb-1 flex items-start justify-between">
        <div>
          <h3 class="text-base font-semibold text-white">Transaction Reports</h3>
          <p class="mt-0.5 text-xs text-slate-500">Transaction reports graph.</p>
        </div>
        <button class="flex items-center gap-1.5 rounded-lg bg-navy-700/60 px-3 py-1.5 text-xs font-medium text-slate-300 transition-colors hover:bg-navy-600">
          Monthly
          <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
          </svg>
        </button>
      </div>

      <!-- Chart -->
      <div class="mt-5 flex gap-3">
        <!-- Y-axis labels -->
        <div class="flex h-48 flex-col justify-between pb-1 text-right">
          @for (label of yLabels; track label) {
            <span class="text-[11px] leading-none text-slate-500">{{ label }}</span>
          }
        </div>

        <!-- Bars + grid -->
        <div class="relative flex-1">
          <!-- Grid lines -->
          <div class="absolute inset-0 flex flex-col justify-between pointer-events-none">
            @for (line of yLabels; track line) {
              <div class="h-px w-full bg-white/5"></div>
            }
          </div>

          <!-- Bars container -->
          <div class="flex h-48 items-end gap-2 px-1">
            @for (bar of chartData; track bar.month) {
              <div class="relative flex flex-1 flex-col items-center">
                <!-- Tooltip for highlighted bar -->
                @if (bar.highlighted) {
                  <div class="absolute -top-8 flex flex-col items-center">
                    <div class="rounded-lg bg-brand px-3 py-1 text-xs font-bold text-white shadow-lg">
                      $750k
                    </div>
                    <div class="h-2 w-2 -mt-px rotate-45 bg-brand"></div>
                  </div>
                }
                <!-- Bar -->
                <div
                  class="w-full rounded-t-lg transition-all duration-300"
                  [style.height]="bar.pct + '%'"
                  [style]="bar.highlighted
                    ? 'height:' + bar.pct + '%; background: repeating-linear-gradient(-45deg, rgba(67,97,238,0.9), rgba(67,97,238,0.9) 4px, rgba(67,97,238,0.35) 4px, rgba(67,97,238,0.35) 8px);'
                    : 'height:' + bar.pct + '%; background-color: #152c44;'"
                ></div>
              </div>
            }
          </div>

          <!-- X-axis labels -->
          <div class="mt-2 flex gap-2 px-1">
            @for (bar of chartData; track bar.month) {
              <div class="flex-1 text-center text-[11px] text-slate-500">{{ bar.month }}</div>
            }
          </div>
        </div>
      </div>
    </div>
  `,
})
export class TransactionChartComponent {
  readonly yLabels = ['1M', '750k', '500k', '250k', '100k']

  readonly chartData: BarData[] = [
    { month: 'Jan', pct: 46, highlighted: false },
    { month: 'Feb', pct: 60, highlighted: false },
    { month: 'Mar', pct: 100, highlighted: true },
    { month: 'Apr', pct: 55, highlighted: false },
    { month: 'May', pct: 64, highlighted: false },
    { month: 'Jun', pct: 40, highlighted: false },
    { month: 'Jul', pct: 50, highlighted: false },
    { month: 'Aug', pct: 68, highlighted: false },
    { month: 'Sep', pct: 38, highlighted: false },
  ]
}
