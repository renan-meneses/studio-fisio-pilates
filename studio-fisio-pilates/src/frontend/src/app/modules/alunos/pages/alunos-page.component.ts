import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { AlunoService } from '../services/aluno.service';
import { Aluno } from '../models/aluno.model';
import { PlanoService } from '../../planos/services/plano.service';
import { Plano } from '../../planos/models/plano.model';

@Component({
  selector: 'clin-alunos-page',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent, ReactiveFormsModule],
  template: `
    <clin-page-header titulo="Alunos" subtitulo="Cadastro de alunos e plano contratado" />

    <form class="card form" [formGroup]="form" (ngSubmit)="salvar()">
      <h3 class="form__title">Novo aluno</h3>
      <div class="form-grid">
        <div class="form-group">
          <label>Nome *</label>
          <input formControlName="nome" placeholder="Nome" />
        </div>
        <div class="form-group">
          <label>Sobrenome</label>
          <input formControlName="sobrenome" placeholder="Sobrenome" />
        </div>
        <div class="form-group form-group--full">
          <label>Endereço</label>
          <input formControlName="endereco" placeholder="Rua, número, bairro, cidade" />
        </div>
        <div class="form-group">
          <label>Telefone</label>
          <input formControlName="telefone" placeholder="(11) 99999-9999" />
        </div>
        <div class="form-group">
          <label>E-mail</label>
          <input type="email" formControlName="email" placeholder="email@exemplo.com" />
        </div>
        <div class="form-group">
          <label>Data de nascimento</label>
          <input type="date" formControlName="dataNascimento" />
        </div>
        <div class="form-group">
          <label>Plano</label>
          <select formControlName="planoId">
            <option value="">Sem plano</option>
            @for (p of planos(); track p.id) {
              <option [value]="p.id">{{ p.nome }} — {{ moeda(p.valor) }}</option>
            }
          </select>
        </div>
      </div>
      @if (erro()) {
        <p class="form__error">{{ erro() }}</p>
      }
      <div class="form__actions">
        <button type="submit" class="btn btn--primary" [disabled]="form.invalid || carregando()">
          {{ carregando() ? 'Salvando…' : 'Cadastrar aluno' }}
        </button>
      </div>
    </form>

    <div class="card">
      @if (carregando()) {
        <p class="hint">Carregando…</p>
      } @else if (alunos().length === 0) {
        <clin-empty-state icone="🎓" titulo="Nenhum aluno cadastrado" hint="Cadastre o primeiro aluno e informe o plano." />
      } @else {
        <table class="data-table">
          <thead>
            <tr>
              <th>Aluno</th>
              <th>Telefone</th>
              <th>E-mail</th>
              <th>Nascimento</th>
              <th>Plano</th>
            </tr>
          </thead>
          <tbody>
            @for (a of alunos(); track a.id) {
              <tr>
                <td>{{ a.nomeCompleto }}</td>
                <td>{{ a.telefone ?? '—' }}</td>
                <td>{{ a.email ?? '—' }}</td>
                <td>{{ a.dataNascimento ? formatarData(a.dataNascimento) : '—' }}</td>
                <td>
                  @if (a.planoNome) {
                    <span class="badge badge--info">{{ a.planoNome }}</span>
                  } @else {
                    <span class="badge badge--muted">Sem plano</span>
                  }
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
    .form-group--full { grid-column: 1 / -1; }
    .form__actions { display: flex; justify-content: flex-end; }
    .form__error { color: var(--clin-danger); font-size: 0.85rem; margin: 0.5rem 0; }
    .hint { color: var(--clin-text-muted); text-align: center; padding: 1rem 0; }
  `,
})
export class AlunosPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(AlunoService);
  private readonly planosService = inject(PlanoService);

  readonly alunos = signal<Aluno[]>([]);
  readonly planos = signal<Plano[]>([]);
  readonly carregando = signal(false);
  readonly erro = signal('');

  readonly form = this.fb.group({
    nome: ['', Validators.required],
    sobrenome: [''],
    endereco: [''],
    telefone: [''],
    email: ['', Validators.email],
    dataNascimento: [''],
    planoId: [''],
  });

  constructor() {
    this.recarregar();
  }

  recarregar(): void {
    this.carregando.set(true);
    this.service.listar().subscribe(lista => this.alunos.set(lista));
    this.planosService.listar().subscribe(lista => this.planos.set(lista));
    this.carregando.set(false);
  }

  moeda(valor: number): string {
    return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  formatarData(iso: string): string {
    const data = new Date(iso);
    return data.toLocaleDateString('pt-BR');
  }

  salvar(): void {
    if (this.form.invalid) {
      return;
    }
    const v = this.form.value;
    this.carregando.set(true);
    this.erro.set('');
    this.service.criar({
      nome: v.nome!,
      sobrenome: v.sobrenome ?? undefined,
      endereco: v.endereco ?? undefined,
      telefone: v.telefone ?? undefined,
      email: v.email ?? undefined,
      dataNascimento: v.dataNascimento ?? undefined,
      planoId: v.planoId || undefined,
    }).subscribe({
      next: () => {
        this.carregando.set(false);
        this.form.reset();
        this.recarregar();
      },
      error: (erro: Error) => {
        this.carregando.set(false);
        this.erro.set(erro.message);
      },
    });
  }
}