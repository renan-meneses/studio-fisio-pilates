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

    <form class="card" [formGroup]="form" (ngSubmit)="salvar()">
      <h3 class="mb-4 text-lg font-bold tracking-tight">Novo plano</h3>
      <div class="grid grid-cols-1 gap-x-4 sm:grid-cols-2">
        <div class="form-group">
          <label>Nome</label>
          <input formControlName="nome" placeholder="Ex.: Pilates 2x por semana" />
        </div>
        <div class="form-group">
          <label>Valor mensal (R$)</label>
          <input type="number" step="0.01" min="0" formControlName="valor" />
        </div>
        <div class="form-group sm:col-span-2">
          <label>Descrição</label>
          <input formControlName="descricao" placeholder="Descrição opcional do plano" />
        </div>
      </div>
      @if (erro()) {
        <p class="field-error my-2">{{ erro() }}</p>
      }
      <div class="flex justify-end">
        <button type="submit" class="btn btn--primary" [disabled]="form.invalid || carregando()">
          {{ carregando() ? 'Salvando…' : 'Salvar plano' }}
        </button>
      </div>
    </form>

    @if (carregando()) {
      <p class="py-4 text-center text-sm text-slate-500 dark:text-slate-400">Carregando…</p>
    } @else if (planos().length === 0) {
      <clin-empty-state icone="💳" titulo="Nenhum plano cadastrado" hint="Crie um plano e adicione os serviços incluídos." />
    } @else {
      <div class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
        @for (p of planos(); track p.id) {
          <div class="card flex flex-col gap-4">
            <div class="flex items-start justify-between gap-4">
              <div>
                <h3 class="text-lg font-bold tracking-tight">{{ p.nome }}</h3>
                @if (p.descricao) {
                  <p class="mt-1 text-sm text-slate-500 dark:text-slate-400">{{ p.descricao }}</p>
                }
              </div>
              <span class="whitespace-nowrap font-extrabold text-teal-700 dark:text-teal-300">{{ moeda(p.valor) }}</span>
            </div>

            <div class="flex flex-col gap-1.5">
              <span class="text-xs font-semibold text-slate-500 dark:text-slate-400">Serviços incluídos:</span>
              @if (p.servicos.length === 0) {
                <p class="m-0 text-sm text-slate-500 dark:text-slate-400">Nenhum serviço adicionado.</p>
              } @else {
                <div class="flex flex-wrap gap-1.5">
                  @for (s of p.servicos; track s.id) {
                    <span class="inline-flex items-center gap-1.5 rounded-full bg-teal-50 px-2.5 py-1 text-xs font-semibold text-teal-700 dark:bg-teal-400/10 dark:text-teal-300">
                      {{ s.nome }}
                      <button
                        class="cursor-pointer border-none bg-transparent p-0 text-[11px] leading-none opacity-70 hover:opacity-100"
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

            <div>
              <select
                class="w-full rounded-lg border border-slate-300 bg-slate-50 px-3 py-2 text-sm text-slate-800 transition-colors focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-500/25 dark:border-slate-700 dark:bg-slate-800/60 dark:text-slate-100"
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