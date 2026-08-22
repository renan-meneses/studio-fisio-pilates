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

    <form class="card" [formGroup]="form" (ngSubmit)="salvar()">
      <h3 class="mb-4 text-base font-semibold text-slate-800 dark:text-slate-100">Novo aluno</h3>
      <div class="grid gap-x-4 sm:grid-cols-2">
        <div class="form-group">
          <label>Nome *</label>
          <input formControlName="nome" placeholder="Nome" />
        </div>
        <div class="form-group">
          <label>Sobrenome</label>
          <input formControlName="sobrenome" placeholder="Sobrenome" />
        </div>
        <div class="form-group sm:col-span-2">
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
        <p class="field-error mt-2">{{ erro() }}</p>
      }
      <div class="mt-2 flex justify-end">
        <button type="submit" class="btn btn--primary" [disabled]="form.invalid || carregando()">
          {{ carregando() ? 'Salvando…' : 'Cadastrar aluno' }}
        </button>
      </div>
    </form>

    <div class="card">
      @if (carregando()) {
        <p class="py-4 text-center text-sm text-slate-500 dark:text-slate-400">Carregando…</p>
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
