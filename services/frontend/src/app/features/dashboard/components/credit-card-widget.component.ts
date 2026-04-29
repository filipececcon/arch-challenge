import { Component } from '@angular/core'

@Component({
  selector: 'app-credit-card-widget',
  standalone: true,
  template: `
    <div class="rounded-2xl border border-navy-700/40 bg-navy-800 p-5">
      <!-- Section header -->
      <div class="mb-4 flex items-center justify-between">
        <h3 class="text-base font-semibold text-white">My Cards</h3>
        <button class="flex items-center gap-1.5 rounded-lg border border-navy-600 px-3 py-1.5 text-xs font-medium text-slate-300 transition-colors hover:border-brand hover:text-white">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
          </svg>
          Add Card
        </button>
      </div>

      <!-- Credit card -->
      <div
        class="relative overflow-hidden rounded-2xl p-5"
        style="background: linear-gradient(135deg, #4cc9f0 0%, #4361ee 45%, #9d4edd 100%); min-height: 160px;"
      >
        <!-- Top row -->
        <div class="mb-6 flex items-start justify-between">
          <!-- NFC/WiFi icon -->
          <svg xmlns="http://www.w3.org/2000/svg" class="h-7 w-7 text-white/80" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
            <path stroke-linecap="round" stroke-linejoin="round" d="M8.288 15.038a5.25 5.25 0 017.424 0M5.106 11.856c3.807-3.808 9.98-3.808 13.788 0M1.924 8.674c5.565-5.565 14.587-5.565 20.152 0M12.53 18.22l-.53.53-.53-.53a.75.75 0 011.06 0z" />
          </svg>
          <!-- Paycent logo -->
          <div class="flex items-center gap-1.5">
            <div class="flex h-5 w-5 items-center justify-center rounded bg-white/20">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-3 w-3 text-white" viewBox="0 0 24 24" fill="currentColor">
                <path d="M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-3.75 3h15a2.25 2.25 0 002.25-2.25V6.75A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25v10.5A2.25 2.25 0 004.5 19.5z"/>
              </svg>
            </div>
            <span class="text-sm font-bold text-white tracking-wide">Paycent</span>
          </div>
        </div>

        <!-- Chip -->
        <div class="mb-5 h-7 w-10 overflow-hidden rounded bg-yellow-200/70 p-0.5">
          <div class="h-full w-full rounded-sm border border-yellow-300/50 bg-gradient-to-br from-yellow-100/50 to-yellow-300/50">
            <div class="mt-1.5 mx-1 h-px bg-yellow-500/40"></div>
            <div class="mt-1 mx-1 h-px bg-yellow-500/40"></div>
          </div>
        </div>

        <!-- Card number -->
        <div class="mb-4 flex gap-3 font-mono text-sm font-semibold tracking-widest text-white">
          <span>6219</span>
          <span>8610</span>
          <span>2888</span>
          <span>8075</span>
        </div>

        <!-- Bottom row -->
        <div class="flex items-end justify-between">
          <div>
            <p class="text-[10px] uppercase tracking-wider text-white/60">Cardholder</p>
            <p class="text-sm font-semibold text-white">Marvin McKinney</p>
          </div>
          <div class="mr-2">
            <p class="text-[10px] uppercase tracking-wider text-white/60">Expires</p>
            <p class="text-sm font-semibold text-white">12/2026</p>
          </div>
          <!-- Mastercard circles -->
          <div class="flex items-center">
            <div class="h-7 w-7 rounded-full bg-red-500/80"></div>
            <div class="-ml-3 h-7 w-7 rounded-full bg-orange-400/80 mix-blend-screen"></div>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class CreditCardWidgetComponent {}
