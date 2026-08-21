import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { SessionStore } from '../../../core/models/session.model';
import { UsuarioService } from '../services/usuario.service';
import { PAPEIS, Papel, Usuario } from '../models/usuario.model';

@Component({
  selector: 'clin-usuarios-page',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent, ReactiveFormsModule, DatePipe],
  template: `
    <clin-page-header titulo="Usuários" subtitulo="Gestão de acessos da clínica" />

    <form class="card form" [formGroup]="form" (ngSubmit)="salvar()">
      <h3 class="form__title">Novo usuário</h3>
      <div class="form-grid">
        <div class="form-group">
          <label>Nome *</label>
          <input formControlName="nome" placeholder="Ex.: Bruna Souza" />
        </div>
        <div class="form-group">
          <label>E-mail *</label>
          <input type="email" formControlName="email" placeholder="bruna@clinica.com" />
        </div>
        <div class="form-group">
          <label>Senha inicial * (mín. 8)</label>
          <input type="password" formControlName="senha" autocomplete="new-password" />
        </div>
        <div class="form-group">
          <label>Papel</label>
          <select formControlName="papel">
            @for (p of papeis; track p.valor) {
              <option [value]="p.valor">{{ p.rotulo }}</option>
            }
          </select>
        </div>
      </div>
      @if (erro()) {
        <p class="form__error">{{ erro() }}</p>
      }
      <div class="form__actions">
        <button type="submit" class="btn btn--primary" [disabled]="form.invalid || carregando()">
          {{ carregando() ? 'Salvando…' : 'Criar usuário' }}
        </button>
      </div>
    </form>

    <section class="card">
      <header class="section">
        <h2 class="section__title">Minha senha</h2>
      </header>
      <form class="senha-form" [formGroup]="formSenha" (ngSubmit)="trocarSenha()">
        <div class="form-group">
          <label>Senha atual</label>
          <input type="password" formControlName="senhaAtual" autocomplete="current-password" />
        </div>
        <div class="form-group">
          <label>Nova senha (mín. 8)</label>
          <input type="password" formControlName="novaSenha" autocomplete="new-password" />
        </div>
        <button type="submit" class="btn btn--outline" [disabled]="formSenha.invalid || carregando()">
          Alterar minha senha
        </button>
      </form>
      @if (mensagemSenha()) {
        <p class="hint-senha">{{ mensagemSenha() }}</p>
      }
    </section>

    <div class="card">
      @if (carregando()) {
        <p class="hint">Carregando…</p>
      } @else if (usuarios().length === 0) {
        <clin-empty-state icone="🔐" titulo="Nenhum usuário" hint="Crie o primeiro acesso da equipe." />
      } @else {
        <table class="data-table">
          <thead>
            <tr>
              <th>Nome</th>
              <th>E-mail</th>
              <th>Papel</th>
              <th>Status</th>
              <th>Último login</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (u of usuarios(); track u.id) {
              <tr>
                <td>{{ u.nome }}</td>
                <td>{{ u.email }}</td>
                <td><span class="badge badge--info">{{ u.papel }}</span></td>
                <td>
                  <span class="badge" [class.badge--success]="u.ativo" [class.badge--danger]="!u.ativo">
                    {{ u.ativo ? 'Ativo' : 'Inativo' }}
                  </span>
                </td>
                <td>{{ u.ultimoLogin ? (u.ultimoLogin | date: 'dd/MM/yyyy HH:mm') : '—' }}</td>
                <td>
                  <div class="acoes">
                    <button
                      class="btn btn--outline"
                      [disabled]="u.id === usuarioAtualId"
                      [title]="u.id === usuarioAtualId ? 'Não é possível desativar a si mesmo' : ''"
                      (click)="alternarStatus(u)"
                    >
                      {{ u.ativo ? 'Desativar' : 'Ativar' }}
                    </button>
                    <button class="btn btn--outline" (click)="redefinirSenha(u)">Redefinir senha</button>
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `,
  styles: `
    .form__title { margin-bottom: 1rem; font-size: 1.05rem; }
    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0 1rem; }
    .form__actions { display: flex; justify-content: flex-end; }
    .form__error { color: var(--clin-danger); font-size: 0.85rem; margin: 0.5rem 0; }
    .hint { color: var(--clin-text-muted); text-align: center; padding: 1rem 0; }
    .section { display: flex; align-items: center; justify-content: space-between; }
    .section__title { margin: 0 0 0.75rem; font-size: 1.05rem; }
    .senha-form { display: flex; align-items: end; gap: 1rem; flex-wrap: wrap; }
    .senha-form .form-group { min-width: 200px; }
    .hint-senha { color: var(--clin-text-muted); font-size: 0.85rem; margin-top: 0.5rem; }
    .acoes { display: flex; gap: 0.4rem; flex-wrap: wrap; }
  `,
})
export class UsuariosPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(UsuarioService);

  readonly papeis = PAPEIS;
  readonly usuarios = signal<Usuario[]>([]);
  readonly carregando = signal(false);
  readonly erro = signal('');
  readonly mensagemSenha = signal('');
  readonly usuarioAtualId: string = '';

  readonly form = this.fb.group({
    nome: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required, Validators.minLength(8)]],
    papel: ['Atendente' as Papel, Validators.required],
  });

  readonly formSenha = this.fb.group({
    senhaAtual: ['', Validators.required],
    novaSenha: ['', [Validators.required, Validators.minLength(8)]],
  });

  constructor() {
    this.usuarioAtualId = SessionStore.userId() ?? '';
    this.recarregar();
  }

  recarregar(): void {
    this.carregando.set(true);
    this.service.listar().subscribe({
      next: lista => {
        this.usuarios.set(lista);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false),
    });
  }

  salvar(): void {
    if (this.form.invalid) {
      return;
    }
    this.carregando.set(true);
    this.erro.set('');
    this.service.criar({
      nome: this.form.value.nome!,
      email: this.form.value.email!,
      senha: this.form.value.senha!,
      papel: this.form.value.papel!,
    }).subscribe({
      next: () => {
        this.carregando.set(false);
        this.form.reset({ papel: 'Atendente' });
        this.recarregar();
      },
      error: (erro: Error) => {
        this.carregando.set(false);
        this.erro.set(erro.message);
      },
    });
  }

  alternarStatus(usuario: Usuario): void {
    const acao = usuario.ativo
      ? `Desativar o acesso de ${usuario.nome}?`
      : `Reativar o acesso de ${usuario.nome}?`;
    if (!confirm(acao)) {
      return;
    }
    this.service.alterarStatus(usuario.id, !usuario.ativo).subscribe({
      next: () => this.recarregar(),
      error: (erro: Error) => alert(erro.message),
    });
  }

  redefinirSenha(usuario: Usuario): void {
    const nova = prompt(`Nova senha para ${usuario.nome} (mín. 8 caracteres):`);
    if (!nova) {
      return;
    }
    if (nova.length < 8) {
      alert('A senha deve ter no mínimo 8 caracteres.');
      return;
    }
    this.service.redefinirSenha(usuario.id, nova).subscribe({
      next: () => alert('Senha redefinida com sucesso.'),
      error: (erro: Error) => alert(erro.message),
    });
  }

  trocarSenha(): void {
    if (this.formSenha.invalid) {
      return;
    }
    this.mensagemSenha.set('');
    this.service.alterarSenhaPropria(
      this.formSenha.value.senhaAtual!,
      this.formSenha.value.novaSenha!,
    ).subscribe({
      next: () => {
        this.mensagemSenha.set('Senha alterada com sucesso.');
        this.formSenha.reset();
      },
      error: (erro: Error) => this.mensagemSenha.set(erro.message),
    });
  }
}
