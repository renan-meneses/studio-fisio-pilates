import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { RhService } from '../services/rh.service';
import { FolhaSalarial, Funcionario } from '../models/rh.model';

function brl(valor: number): string {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

@Component({
  selector: 'clin-folha-page',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent, ReactiveFormsModule],
  template: `
    <clin-page-header titulo="Folha de pagamento" subtitulo="Processamento mensal dos salários">
      <input
        type="month"
        class="rounded-lg border border-slate-200 bg-white px-3 py-2 dark:border-slate-800 dark:bg-slate-900"
        [value]="competencia()"
        (change)="mudarCompetencia($event)"
      />
    </clin-page-header>

    <section class="card">
      <h2 class="mb-3 text-lg font-semibold text-slate-900 dark:text-slate-100">Processar folha</h2>
      <form [formGroup]="form" (ngSubmit)="processar()" class="grid grid-cols-[2fr_1fr_1fr_auto] items-end gap-4">
        <div class="form-group">
          <label>Funcionário *</label>
          <select formControlName="funcionarioId">
            <option value="" disabled>Selecione…</option>
            @for (f of funcionarios(); track f.id) {
              <option [value]="f.id">{{ f.nome }} — {{ f.cargo }}</option>
            }
          </select>
        </div>
        <div class="form-group">
          <label>Competência</label>
          <input type="month" formControlName="competencia" />
        </div>
        <div class="form-group">
          <label>Descontos (R$)</label>
          <input formControlName="descontos" type="number" step="0.01" min="0" />
        </div>
        <button class="btn btn--primary" type="submit" [disabled]="form.invalid">Processar</button>
      </form>
    </section>

    <section class="card">
      <h2 class="mb-3 text-lg font-semibold text-slate-900 dark:text-slate-100">Folha de {{ competencia() }}</h2>
      @if (folha().length === 0) {
        <clin-empty-state
          icone="📄"
          titulo="Nenhuma folha processada"
          hint="Processe a folha do mês para liberar o pagamento dos profissionais."
        />
      } @else {
        <table class="data-table">
          <thead>
            <tr>
              <th>Funcionário</th>
              <th>Bruto</th>
              <th>Descontos</th>
              <th>Líquido</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (f of folha(); track f.id) {
              <tr>
                <td>{{ f.funcionarioNome }}</td>
                <td>{{ brl(f.salarioBruto) }}</td>
                <td>{{ brl(f.descontos) }}</td>
                <td><strong>{{ brl(f.salarioLiquido) }}</strong></td>
                <td>
                  <span class="badge {{ f.paga ? 'badge--success' : 'badge--warning' }}">
                    {{ f.paga ? 'Paga' : 'Pendente' }}
                  </span>
                </td>
                <td>
                  @if (!f.paga) {
                    <button class="btn btn--primary" (click)="pagar(f)">Pagar</button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
})
export class FolhaPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly rh = inject(RhService);

  readonly brl = brl;
  readonly competencia = signal(new Date().toISOString().slice(0, 7));
  readonly funcionarios = signal<Funcionario[]>([]);
  readonly folha = signal<FolhaSalarial[]>([]);

  readonly form = this.fb.group({
    funcionarioId: ['', Validators.required],
    competencia: ['', Validators.required],
    descontos: [0, [Validators.min(0)]],
  });

  constructor() {
    this.recarregar();
    this.rh.listarFuncionarios().subscribe(f => this.funcionarios.set(f));
  }

  mudarCompetencia(evento: Event): void {
    this.competencia.set((evento.target as HTMLInputElement).value);
    this.recarregar();
  }

  recarregar(): void {
    this.rh.listarFolha(this.competencia()).subscribe(folha => this.folha.set(folha));
  }

  processar(): void {
    const v = this.form.value;
    this.rh
      .processarFolha(v.funcionarioId!, {
        competencia: v.competencia!,
        descontos: v.descontos ?? 0,
      })
      .subscribe(() => {
        this.recarregar();
        this.form.patchValue({ descontos: 0 });
      });
  }

  pagar(f: FolhaSalarial): void {
    this.rh.pagarFolha(f.id).subscribe(() => this.recarregar());
  }
}