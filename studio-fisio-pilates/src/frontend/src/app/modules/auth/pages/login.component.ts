import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, Credenciais } from '../../../core/services/auth.service';
import { TenantService } from '../../../core/services/tenant.service';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'clin-login',
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
            ✚
          </div>
          <h1 class="text-xl font-extrabold tracking-tight">Entrar</h1>
          <p class="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Gestão de fisioterapia e pilates
          </p>
        </div>

        <div class="form-group">
          <label for="email">E-mail</label>
          <input id="email" type="email" formControlName="email" autocomplete="email" placeholder="voce@clinica.com" />
        </div>

        <div class="form-group">
          <label for="senha">Senha</label>
          <input
            id="senha"
            type="password"
            formControlName="senha"
            autocomplete="current-password"
            placeholder="••••••••"
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
          {{ carregando() ? 'Entrando…' : 'Entrar' }}
        </button>

        <a routerLink="/recuperar-senha" class="mt-4 block text-center text-xs font-semibold text-teal-600 hover:underline dark:text-teal-400">
          Esqueci minha senha
        </a>
      </form>
    </div>
  `,
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly tenant = inject(TenantService);
  private readonly theme = inject(ThemeService);
  private readonly router = inject(Router);

  readonly carregando = signal(false);
  readonly erro = signal('');

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    senha: ['', Validators.required],
  });

  onSubmit(): void {
    if (this.form.invalid) {
      return;
    }
    this.carregando.set(true);
    this.erro.set('');
    this.auth.login(this.form.getRawValue() as Credenciais).subscribe({
      next: login => {
        this.tenant.setTenant({ tenantId: login.tenantId, tenantNome: login.tenantNome });
        this.theme.sincronizar(login.tema);
        this.carregando.set(false);
        void this.router.navigate(['/agenda']);
      },
      error: (erro: Error) => {
        this.carregando.set(false);
        this.erro.set(erro.message);
      },
    });
  }
}
