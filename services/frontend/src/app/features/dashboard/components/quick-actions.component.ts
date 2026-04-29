import { Component } from '@angular/core'

interface Action {
  label: string
  icon: string
}

@Component({
  selector: 'app-quick-actions',
  standalone: true,
  template: `
    <div class="grid grid-cols-4 gap-3">
      @for (action of actions; track action.label) {
        <button class="flex flex-col items-center gap-2 transition-opacity hover:opacity-80">
          <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-navy-800 border border-navy-700/40 transition-colors hover:border-brand/60 hover:bg-navy-700">
            @switch (action.label) {
              @case ('Add') {
                <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                </svg>
              }
              @case ('Send') {
                <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 10.5L12 3m0 0l7.5 7.5M12 3v18" />
                </svg>
              }
              @case ('Received') {
                <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 13.5L12 21m0 0l-7.5-7.5M12 21V3" />
                </svg>
              }
              @case ('History') {
                <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              }
            }
          </div>
          <span class="text-xs text-slate-400">{{ action.label }}</span>
        </button>
      }
    </div>
  `,
})
export class QuickActionsComponent {
  readonly actions: Action[] = [
    { label: 'Add', icon: 'plus' },
    { label: 'Send', icon: 'arrow-up' },
    { label: 'Received', icon: 'arrow-down' },
    { label: 'History', icon: 'clock' },
  ]
}
