import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'clin-recuperar-senha',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="login-page">
      <form class="login-card" [formGroup]="form" (ngSubmit)="onSubmit()">
        <div class="login-card__brand">
          <div class="login-card__logo">Clínica<span>SaaS</span></div>
          <h1>Recuperar senha</h1>
          <p>Enviaremos um token de redefinição para o seu e-mail</p>
        </div>

        <div class="form-group">
          <label for="email">E-mail</label>
          <input id="email" type="email" formControlName="email" autocomplete="email" />
        </div>

        @if (enviado()) {
          <p class="login-card__ok">
            Se o e-mail estiver cadastrado, você receberá as instruções em instantes.
          </p>
        }

        <button class="btn btn--primary login-card__submit" type="submit" [disabled]="form.invalid || carregando()">
          {{ carregando() ? 'Enviando…' : enviado() ? 'Reenviar' : 'Enviar instruções' }}
        </button>

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
    .login-card__ok { color: var(--clin-success, #16a34a); font-size: 0.85rem; margin: 0.5rem 0; }
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
