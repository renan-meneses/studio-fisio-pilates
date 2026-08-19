import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, Credenciais } from '../../../core/services/auth.service';
import { TenantService } from '../../../core/services/tenant.service';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'clin-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="login-page">
      <form class="login-card" [formGroup]="form" (ngSubmit)="onSubmit()">
        <div class="login-card__brand">
          <div class="login-card__logo">Clínica<span>SaaS</span></div>
          <h1>Entrar</h1>
          <p>Gestão de fisioterapia e pilates</p>
        </div>

        <div class="form-group">
          <label for="email">E-mail</label>
          <input id="email" type="email" formControlName="email" autocomplete="email" />
        </div>

        <div class="form-group">
          <label for="senha">Senha</label>
          <input id="senha" type="password" formControlName="senha" autocomplete="current-password" />
        </div>

        @if (erro()) {
          <p class="login-card__error">{{ erro() }}</p>
        }

        <button class="btn btn--primary login-card__submit" type="submit" [disabled]="form.invalid || carregando()">
          {{ carregando() ? 'Entrando…' : 'Entrar' }}
        </button>
      </form>
    </div>
  `,
  styles: `
    .login-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(160deg, #0f766e 0%, #134e4a 55%, #0f172a 100%);
      padding: 1rem;
    }
    .login-card {
      width: 100%;
      max-width: 380px;
      background: var(--clin-surface);
      border-radius: 16px;
      padding: 2rem;
      box-shadow: 0 20px 50px rgba(0, 0, 0, 0.35);
    }
    .login-card__brand { margin-bottom: 1.5rem; }
    .login-card__logo { font-size: 1.15rem; font-weight: 800; color: var(--clin-primary); }
    .login-card__logo span { color: var(--clin-accent); }
    .login-card h1 { font-size: 1.4rem; margin-top: 0.75rem; }
    .login-card p { margin: 0.25rem 0 0; color: var(--clin-text-muted); }
    .login-card__submit { width: 100%; justify-content: center; margin-top: 0.5rem; }
    .login-card__error { color: var(--clin-danger); font-size: 0.85rem; margin: 0.5rem 0; }
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