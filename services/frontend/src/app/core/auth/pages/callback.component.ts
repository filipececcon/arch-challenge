import { Component, OnInit, inject, signal } from '@angular/core'
import { ActivatedRoute, Router, RouterLink } from '@angular/router'
import { AuthService } from '../auth.service'
import {
  clearPkceSession,
  getPkceVerifierIfStateValid,
} from '../transient-oidc'

@Component({
  selector: 'app-callback',
  standalone: true,
  imports: [RouterLink],
  template: `
    @if (error(); as msg) {
      <div class="flex min-h-dvh flex-col items-center justify-center px-4">
        <div
          class="max-w-lg rounded-xl border border-red-900/50 bg-red-950/40 p-6 text-center"
        >
          <h1 class="text-lg font-medium text-red-200">Não foi possível entrar</h1>
          <p class="mt-2 text-sm text-red-300/90">{{ msg }}</p>
          <a
            routerLink="/login"
            class="mt-6 inline-block text-sm text-sky-400 underline hover:text-sky-300"
          >
            Voltar ao login
          </a>
        </div>
      </div>
    } @else {
      <div class="flex min-h-dvh items-center justify-center">
        <p class="text-sm text-slate-400">Concluindo login…</p>
      </div>
    }
  `,
})
export class CallbackComponent implements OnInit {
  private readonly route = inject(ActivatedRoute)
  private readonly router = inject(Router)
  private readonly auth = inject(AuthService)

  protected readonly error = signal<string | null>(null)

  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap
    const err = q.get('error')
    const errDesc = q.get('error_description')
    if (err) {
      this.error.set(
        errDesc
          ? `${err}: ${errDesc.replace(/\+/g, ' ')}`
          : `Erro OIDC: ${err}`,
      )
      return
    }

    const code = q.get('code')
    const state = q.get('state')
    if (!code || !state) {
      this.error.set('Resposta inválida: faltam code ou state.')
      return
    }

    const verifier = getPkceVerifierIfStateValid(state)
    if (!verifier) {
      this.error.set(
        'Sessão PKCE inválida ou expirada. Tente entrar novamente.',
      )
      return
    }

    void this.auth.completeLoginWithCode(code, verifier).then(
      () => {
        clearPkceSession()
        void this.router.navigateByUrl('/', { replaceUrl: true })
      },
      (e: unknown) => {
        this.auth.resetLastExchangeAttempt()
        this.error.set(
          e instanceof Error ? e.message : 'Falha ao obter tokens.',
        )
      },
    )
  }
}
