import { Component, inject } from '@angular/core'
import { RouterOutlet } from '@angular/router'
import { AuthService } from '../core/auth/auth.service'
import { SidebarComponent } from './sidebar.component'

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent],
  template: `
    @if (auth.state(); as s) {
      @if (s.kind === 'authenticated') {
        <div class="flex h-screen overflow-hidden bg-navy-900">
          <app-sidebar />
          <main class="flex flex-1 flex-col overflow-hidden">
            <router-outlet />
          </main>
        </div>
      }
    }
  `,
})
export class AppShellComponent {
  readonly auth = inject(AuthService)
}
