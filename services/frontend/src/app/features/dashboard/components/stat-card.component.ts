import { Component, Input } from '@angular/core'
import { NgClass } from '@angular/common'

@Component({
  selector: 'app-stat-card',
  standalone: true,
  imports: [NgClass],
  template: `
    <div class="rounded-2xl border border-navy-700/40 bg-navy-800 p-5">
      <!-- Header -->
      <div class="mb-4 flex items-center justify-between">
        <span class="text-sm text-slate-400">{{ title }}</span>
        <button class="text-slate-500 transition-colors hover:text-white">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="currentColor" viewBox="0 0 24 24">
            <circle cx="5" cy="12" r="1.5"/>
            <circle cx="12" cy="12" r="1.5"/>
            <circle cx="19" cy="12" r="1.5"/>
          </svg>
        </button>
      </div>

      <!-- Value + icon -->
      <div class="flex items-end justify-between gap-3">
        <div class="min-w-0">
          <p class="mb-2.5 text-2xl font-bold text-white">{{ value }}</p>
          <!-- Change badge -->
          <div
            class="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium"
            [ngClass]="positive
              ? 'bg-emerald-500/15 text-emerald-400'
              : 'bg-red-500/15 text-red-400'"
          >
            <svg xmlns="http://www.w3.org/2000/svg" class="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
              @if (positive) {
                <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 10.5L12 3m0 0l7.5 7.5M12 3v18" />
              } @else {
                <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 13.5L12 21m0 0l-7.5-7.5M12 21V3" />
              }
            </svg>
            {{ change }} {{ changeLabel }}
          </div>
        </div>

        <!-- Icon -->
        <div
          class="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-xl"
          [ngClass]="positive ? 'bg-emerald-500/10' : 'bg-red-500/10'"
        >
          <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5"
            [ngClass]="positive ? 'text-emerald-400' : 'text-red-400'">
            <path stroke-linecap="round" stroke-linejoin="round" d="M20.25 6.375c0 2.278-3.694 4.125-8.25 4.125S3.75 8.653 3.75 6.375m16.5 0c0-2.278-3.694-4.125-8.25-4.125S3.75 4.097 3.75 6.375m16.5 0v11.25c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125V6.375m16.5 0v3.75m-16.5-3.75v3.75m16.5 0v3.75C20.25 16.153 16.556 18 12 18s-8.25-1.847-8.25-4.125v-3.75m16.5 0c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125" />
          </svg>
        </div>
      </div>
    </div>
  `,
})
export class StatCardComponent {
  @Input({ required: true }) title = ''
  @Input({ required: true }) value = ''
  @Input({ required: true }) change = ''
  @Input({ required: true }) changeLabel = ''
  @Input() positive = true
}
