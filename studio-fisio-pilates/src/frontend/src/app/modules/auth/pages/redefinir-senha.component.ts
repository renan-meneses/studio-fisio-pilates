import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'clin-redefinir-senha',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="login-page">
      <form class="login-card" [formGroup]="form" (ngSubmit)="onSubmit()">
        <div class="login-card__brand">
          <div class="login-card__logo">Clínica<span>SaaS</span></div>
          <h1>Redefinir senha</h1>
          <p>Digite a nova senha para concluir</p>
        </div>

        @if (!tokenValido()) {
          <p class="login-card__error">
            Link inválido. Solicite uma nova redefinição de senha.
          </p>
          <a class="login-card__link" routerLink="/recuperar-senha">Solicitar novo token</a>
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
            <p class="login-card__error">{{ erro() }}</p>
          }

          <button
            class="btn btn--primary login-card__submit"
            type="submit"
            [disabled]="form.invalid || carregando()"
          >
            {{ carregando() ? 'Redefinindo…' : 'Redefinir senha' }}
          </button>
        }

        <a class="login-card__link" routerLink="/login">Voltar ao login</a>
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
    .login-card__link {
      display: block;
      margin-top: 1rem;
      text-align: center;
      color: var(--clin-primary);
      font-size: 0.85rem;
      text-decoration: none;
    }
  `,
})
export class RedefinirSenhaComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly carregando = signal(false);
  readonly erro = signal('');
  readonly concluido = signal(false);

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
