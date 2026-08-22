import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'clin-recuperar-senha',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div
      class="flex min-h-screen items-center justify-center bg-gradient-to-br from-teal-600 via-teal-800 to-slate-950 p-4"
    >
      <form
        class="w-full max-w-sm rounded-2xl border border-white/10 bg-white p-8 shadow-pop dark:bg-slate-900 animate-scale-in"
        [formGroup]="form"
        (ngSubmit)="onSubmit()"
      >
        <div class="mb-7 text-center">
          <div
            class="mx-auto mb-3 flex size-12 items-center justify-center rounded-2xl bg-gradient-to-br from-teal-400 to-teal-600 text-xl text-white shadow-lg shadow-teal-500/25"
          >
            🔑
          </div>
          <h1 class="text-xl font-extrabold tracking-tight">Recuperar senha</h1>
          <p class="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Enviaremos um token de redefinição para o seu e-mail
          </p>
        </div>

        <div class="form-group">
          <label for="email">E-mail</label>
          <input id="email" type="email" formControlName="email" autocomplete="email" placeholder="voce@clinica.com" />
        </div>

        @if (enviado()) {
          <p class="mb-4 rounded-lg bg-emerald-50 px-3 py-2.5 text-xs leading-relaxed text-emerald-700 ring-1 ring-inset ring-emerald-600/20 dark:bg-emerald-400/10 dark:text-emerald-300 dark:ring-emerald-400/20">
            Se o e-mail estiver cadastrado, você receberá as instruções em instantes.
          </p>
        }

        <button
          class="btn btn--primary w-full !py-2.5"
          type="submit"
          [disabled]="form.invalid || carregando()"
        >
          {{ carregando() ? 'Enviando…' : enviado() ? 'Reenviar' : 'Enviar instruções' }}
        </button>

        <a routerLink="/login" class="mt-4 block text-center text-xs font-semibold text-teal-600 hover:underline dark:text-teal-400">
          Voltar ao login
        </a>
      </form>
    </div>
  `,
})
export class RecuperarSenhaComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly carregando = signal(false);
  readonly enviado = signal(false);

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
  });

  onSubmit(): void {
    if (this.form.invalid) {
      return;
    }
    this.carregando.set(true);
    this.auth.solicitarRedefinicao(this.form.getRawValue().email!).subscribe({
      next: () => {
        // Mensagem genérica mesmo para e-mail inexistente (anti-enumeração).
        this.enviado.set(true);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false),
    });
  }
}
