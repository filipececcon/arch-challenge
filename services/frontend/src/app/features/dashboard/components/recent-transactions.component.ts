import { Component } from '@angular/core'
import { NgClass } from '@angular/common'

interface Transaction {
  id: number
  name: string
  category: string
  initials: string
  bgColor: string
  amount: string
  isDebit: boolean
  date: string
  status: 'Success' | 'Pending' | 'Failed'
  time: string
}

@Component({
  selector: 'app-recent-transactions',
  standalone: true,
  imports: [NgClass],
  template: `
    <div class="rounded-2xl border border-navy-700/40 bg-navy-800 p-5">
      <!-- Header -->
      <div class="mb-5 flex items-center justify-between">
        <h3 class="text-base font-semibold text-white">Recent Transactions</h3>
        <button class="flex items-center gap-1.5 rounded-lg bg-navy-700/60 px-3 py-1.5 text-xs font-medium text-slate-300 transition-colors hover:bg-navy-600">
          Today
          <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
          </svg>
        </button>
      </div>

      <!-- Table header -->
      <div class="mb-3 grid grid-cols-[1fr_auto_auto_auto_auto] gap-4 px-1 text-xs font-medium uppercase tracking-wide text-slate-500">
        <span>Account</span>
        <span class="text-right">Amount</span>
        <span>Date</span>
        <span>Status</span>
        <span>Time</span>
      </div>

      <!-- Rows -->
      <div class="flex flex-col gap-3">
        @for (tx of transactions; track tx.id) {
          <div class="grid grid-cols-[1fr_auto_auto_auto_auto] items-center gap-4 rounded-xl px-1 py-2 transition-colors hover:bg-navy-700/30">
            <!-- Account -->
            <div class="flex min-w-0 items-center gap-3">
              <div
                class="flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-full text-sm font-bold text-white"
                [style.background-color]="tx.bgColor"
              >
                {{ tx.initials }}
              </div>
              <div class="min-w-0">
                <p class="truncate text-sm font-medium text-white">{{ tx.name }}</p>
                <p class="text-xs text-slate-500">{{ tx.category }}</p>
              </div>
            </div>

            <!-- Amount -->
            <div class="flex items-center gap-1">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-3 w-3 text-red-400" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 4l-8 8h5v8h6v-8h5z"/>
              </svg>
              <span class="text-sm font-semibold text-white">{{ tx.amount }}</span>
            </div>

            <!-- Date -->
            <span class="text-xs text-slate-400">{{ tx.date }}</span>

            <!-- Status badge -->
            <span
              class="rounded-full px-3 py-1 text-xs font-semibold"
              [ngClass]="{
                'bg-emerald-500/20 text-emerald-400': tx.status === 'Success',
                'bg-amber-500/20 text-amber-400': tx.status === 'Pending',
                'bg-red-500/20 text-red-400': tx.status === 'Failed'
              }"
            >
              {{ tx.status }}
            </span>

            <!-- Time -->
            <span class="text-xs text-slate-500">{{ tx.time }}</span>
          </div>
        }
      </div>
    </div>
  `,
})
export class RecentTransactionsComponent {
  readonly transactions: Transaction[] = [
    {
      id: 1,
      name: 'Figma Pro Plan',
      category: 'Figma',
      initials: 'F',
      bgColor: '#7c3aed',
      amount: '$5,000',
      isDebit: true,
      date: 'Dec 12, 2024',
      status: 'Success',
      time: '5 min ago',
    },
    {
      id: 2,
      name: 'Youtube Pro Plan',
      category: 'Youtube',
      initials: 'Y',
      bgColor: '#ef4444',
      amount: '$2,500',
      isDebit: true,
      date: 'Dec 12, 2024',
      status: 'Pending',
      time: '7 min ago',
    },
    {
      id: 3,
      name: 'Spotify Premium',
      category: 'Spotify',
      initials: 'S',
      bgColor: '#22c55e',
      amount: '$980',
      isDebit: true,
      date: 'Dec 11, 2024',
      status: 'Success',
      time: '1 hr ago',
    },
  ]
}
