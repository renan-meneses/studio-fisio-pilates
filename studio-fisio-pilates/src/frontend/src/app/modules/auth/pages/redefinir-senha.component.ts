import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'clin-redefinir-senha',
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
            🔒
          </div>
          <h1 class="text-xl font-extrabold tracking-tight">Redefinir senha</h1>
          <p class="mt-1 text-sm text-slate-500 dark:text-slate-400">Digite a nova senha para concluir</p>
        </div>

        @if (!tokenValido()) {
          <p class="field-error" role="alert">Link inválido. Solicite uma nova redefinição de senha.</p>
          <a
            routerLink="/recuperar-senha"
            class="mt-4 block text-center text-xs font-semibold text-teal-600 hover:underline dark:text-teal-400"
          >
            Solicitar novo token
          </a>
        } @else {
          <div class="form-group">
            <label for="senha">Nova senha</label>
            <input
              id="senha"
              type="password"
              formControlName="senha"
              autocomplete="new-password"
              placeholder="Mínimo de 8 caracteres"
            />
          </div>

          @if (erro()) {
            <p class="field-error" role="alert">{{ erro() }}</p>
          }

          <button
            class="btn btn--primary mt-2 w-full !py-2.5"
            type="submit"
            [disabled]="form.invalid || carregando()"
          >
            {{ carregando() ? 'Redefinindo…' : 'Redefinir senha' }}
          </button>
        }

        <a routerLink="/login" class="mt-4 block text-center text-xs font-semibold text-teal-600 hover:underline dark:text-teal-400">
          Voltar ao login
        </a>
      </form>
    </div>
  `,
})
export class RedefinirSenhaComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly carregando = signal(false);
  readonly erro = signal('');

  readonly email = this.route.snapshot.queryParamMap.get('email') ?? '';
  readonly token = this.route.snapshot.queryParamMap.get('token') ?? '';

  readonly tokenValido = signal(this.email.length > 0 && this.token.length > 0);

  readonly form = this.fb.group({
    senha: ['', [Validators.required, Validators.minLength(8)]],
  });

  onSubmit(): void {
    if (this.form.invalid) {
      return;
    }
    this.carregando.set(true);
    this.erro.set('');
    this.auth.redefinirSenha(this.email, this.token, this.form.getRawValue().senha!).subscribe({
      next: () => {
        this.carregando.set(false);
        void this.router.navigate(['/login']);
      },
      error: (e: Error) => {
        this.carregando.set(false);
        this.erro.set(e.message);
      },
    });
  }
}
