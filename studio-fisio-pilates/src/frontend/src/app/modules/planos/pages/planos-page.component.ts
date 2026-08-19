import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { PlanoService } from '../services/plano.service';
import { Plano } from '../models/plano.model';
import { ServicoService } from '../../servicos/services/servico.service';
import { Servico } from '../../servicos/models/servico.model';

function moeda(valor: number): string {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

@Component({
  selector: 'clin-planos-page',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent, ReactiveFormsModule],
  template: `
    <clin-page-header titulo="Planos" subtitulo="Planos comerciais e serviços incluídos" />

    <form class="card form" [formGroup]="form" (ngSubmit)="salvar()">
      <h3 class="form__title">Novo plano</h3>
      <div class="form-grid">
        <div class="form-group">
          <label>Nome</label>
          <input formControlName="nome" placeholder="Ex.: Pilates 2x por semana" />
        </div>
        <div class="form-group">
          <label>Valor mensal (R$)</label>
          <input type="number" step="0.01" min="0" formControlName="valor" />
        </div>
        <div class="form-group form-group--full">
          <label>Descrição</label>
          <input formControlName="descricao" placeholder="Descrição opcional do plano" />
        </div>
      </div>
      @if (erro()) {
        <p class="form__error">{{ erro() }}</p>
      }
      <div class="form__actions">
        <button type="submit" class="btn btn--primary" [disabled]="form.invalid || carregando()">
          {{ carregando() ? 'Salvando…' : 'Salvar plano' }}
        </button>
      </div>
    </form>

    @if (carregando()) {
      <p class="hint">Carregando…</p>
    } @else if (planos().length === 0) {
      <clin-empty-state icone="💳" titulo="Nenhum plano cadastrado" hint="Crie um plano e adicione os serviços incluídos." />
    } @else {
      <div class="planos">
        @for (p of planos(); track p.id) {
          <div class="card plano">
            <div class="plano__head">
              <div>
                <h3 class="plano__nome">{{ p.nome }}</h3>
                @if (p.descricao) {
                  <p class="plano__descricao">{{ p.descricao }}</p>
                }
              </div>
              <span class="plano__valor">{{ moeda(p.valor) }}</span>
            </div>

            <div class="plano__servicos">
              <span class="plano__label">Serviços incluídos:</span>
              @if (p.servicos.length === 0) {
                <p class="plano__vazio">Nenhum serviço adicionado.</p>
              } @else {
                <div class="chip">
                  @for (s of p.servicos; track s.id) {
                    <span class="chip__item">
                      {{ s.nome }}
                      <button
                        class="chip__remover"
                        title="Remover serviço do plano"
                        (click)="remover(p, s)"
                      >
                        ✕
                      </button>
                    </span>
                  }
                </div>
              }
            </div>

            <div class="plano__add">
              <select
                class="plano__select"
                [value]="''"
                (change)="adicionar(p, $event)"
              >
                <option value="" disabled>+ Adicionar serviço…</option>
                @for (s of servicosDisponiveis(p); track s.id) {
                  <option [value]="s.id">{{ s.nome }} — {{ moeda(s.valor) }}</option>
                }
              </select>
            </div>
          </div>
        }
      </div>
    }
  `,
  styles: `
    .form__title { margin-bottom: 1rem; font-size: 1.05rem; }
    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0 1rem; }
    .form-group--full { grid-column: 1 / -1; }
    .form__actions { display: flex; justify-content: flex-end; }
    .form__error { color: var(--clin-danger); font-size: 0.85rem; margin: 0.5rem 0; }
    .hint { color: var(--clin-text-muted); text-align: center; padding: 1rem 0; }
    .planos { display: grid; grid-template-columns: repeat(auto-fill, minmax(340px, 1fr)); gap: 1rem; }
    .plano { display: flex; flex-direction: column; gap: 1rem; }
    .plano__head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; }
    .plano__nome { font-size: 1.05rem; }
    .plano__descricao { margin: 0.25rem 0 0; color: var(--clin-text-muted); font-size: 0.85rem; }
    .plano__valor { font-weight: 800; color: var(--clin-primary-dark); white-space: nowrap; }
    .plano__servicos { display: flex; flex-direction: column; gap: 0.4rem; }
    .plano__label { font-size: 0.8rem; font-weight: 600; color: var(--clin-text-muted); }
    .plano__vazio { margin: 0; color: var(--clin-text-muted); font-size: 0.85rem; }
    .chip { display: flex; flex-wrap: wrap; gap: 0.4rem; }
    .chip__item {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      background: var(--clin-primary-light);
      color: var(--clin-primary-dark);
      padding: 0.3rem 0.6rem;
      border-radius: 999px;
      font-size: 0.8rem;
      font-weight: 600;
    }
    .chip__remover {
      border: none;
      background: transparent;
      color: inherit;
      cursor: pointer;
      font-size: 0.75rem;
      padding: 0;
      opacity: 0.7;

      &:hover { opacity: 1; }
    }
    .plano__select {
      width: 100%;
      padding: 0.5rem 0.75rem;
      border: 1px solid var(--clin-border);
      border-radius: 8px;
      font: inherit;
      background: var(--clin-surface-alt);
      color: var(--clin-text);
    }
  `,
})
export class PlanosPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly planosService = inject(PlanoService);
  private readonly servicosService = inject(ServicoService);

  readonly planos = signal<Plano[]>([]);
  readonly servicos = signal<Servico[]>([]);
  readonly carregando = signal(false);
  readonly erro = signal('');

  readonly form = this.fb.group({
    nome: ['', Validators.required],
    valor: [0, [Validators.required, Validators.min(0)]],
    descricao: [''],
  });

  constructor() {
    this.recarregar();
  }

  recarregar(): void {
    this.carregando.set(true);
    this.planosService.listar().subscribe(lista => this.planos.set(lista));
    this.servicosService.listar().subscribe(lista => this.servicos.set(lista));
    this.carregando.set(false);
  }

  moeda = moeda;

  servicosDisponiveis(plano: Plano): Servico[] {
    const incluidos = new Set(plano.servicos.map(s => s.id));
    return this.servicos().filter(s => s.ativo && !incluidos.has(s.id));
  }

  salvar(): void {
    if (this.form.invalid) {
      return;
    }
    this.erro.set('');
    this.planosService.criar({
      nome: this.form.value.nome!,
      valor: this.form.value.valor ?? 0,
      descricao: this.form.value.descricao ?? undefined,
    }).subscribe({
      next: () => {
        this.form.reset({ valor: 0 });
        this.recarregar();
      },
      error: (erro: Error) => this.erro.set(erro.message),
    });
  }

  adicionar(plano: Plano, evento: Event): void {
    const servicoId = (evento.target as HTMLSelectElement).value;
    if (!servicoId) {
      return;
    }
    this.planosService.adicionarServico(plano.id, servicoId).subscribe({
      next: () => this.recarregar(),
      error: (erro: Error) => alert(erro.message),
    });
  }

  remover(plano: Plano, servico: Servico): void {
    this.planosService.removerServico(plano.id, servico.id).subscribe({
      next: () => this.recarregar(),
      error: (erro: Error) => alert(erro.message),
    });
  }
}