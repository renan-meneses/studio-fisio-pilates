import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { RhService } from '../services/rh.service';
import { Funcionario, RegistroPonto } from '../models/rh.model';

@Component({
  selector: 'clin-ponto-page',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent, ReactiveFormsModule],
  template: `
    <clin-page-header titulo="Controle de ponto" subtitulo="Registro de entrada e saída">

    </clin-page-header>

    <section class="card">
      <h2 class="titulo">Cadastrar funcionário</h2>
      <form [formGroup]="funcionarioForm" (ngSubmit)="cadastrarFuncionario()" class="grid">
        <div class="form-group">
          <label>Nome *</label>
          <input formControlName="nome" />
        </div>
        <div class="form-group">
          <label>Cargo *</label>
          <input formControlName="cargo" placeholder="Ex.: Fisioterapeuta" />
        </div>
        <button class="btn btn--primary" type="submit" [disabled]="funcionarioForm.invalid">
          Cadastrar
        </button>
      </form>
    </section>

    <section class="card">
      <h2 class="titulo">Lançar ponto</h2>
      <form [formGroup]="pontoForm" (ngSubmit)="lancarPonto()" class="grid">
        <div class="form-group">
          <label>Funcionário *</label>
          <select formControlName="funcionarioId">
            <option value="" disabled>Selecione…</option>
            @for (f of funcionarios(); track f.id) {
              <option [value]="f.id">{{ f.nome }}</option>
            }
          </select>
        </div>
        <div class="form-group">
          <label>Data *</label>
          <input type="date" formControlName="data" />
        </div>
        <div class="form-group">
          <label>Entrada *</label>
          <input type="time" formControlName="horaEntrada" />
        </div>
        <div class="form-group">
          <label>Saída</label>
          <input type="time" formControlName="horaSaida" />
        </div>
        <button class="btn btn--primary" type="submit" [disabled]="pontoForm.invalid">Lançar</button>
      </form>
    </section>

    <section class="card">
      <h2 class="titulo">Registros do dia</h2>
      @if (registros().length === 0) {
        <clin-empty-state
          icone="🕐"
          titulo="Nenhum ponto registrado hoje"
          hint="Lance entradas e saídas dos profissionais."
        />
      } @else {
        <table class="data-table">
          <thead>
            <tr>
              <th>Data</th>
              <th>Entrada</th>
              <th>Saída</th>
            </tr>
          </thead>
          <tbody>
            @for (p of registros(); track p.id) {
              <tr>
                <td>{{ p.data }}</td>
                <td>{{ p.horaEntrada }}</td>
                <td>{{ p.horaSaida ?? '—' }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
  styles: `
    .titulo { font-size: 1.05rem; margin-bottom: 0.75rem; }
    .grid { display: grid; grid-template-columns: 2fr 1fr 1fr 1fr auto; gap: 1rem; align-items: end; }
  `,
})
export class PontoPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly rh = inject(RhService);

  readonly funcionarios = signal<Funcionario[]>([]);
  readonly registros = signal<RegistroPonto[]>([]);

  readonly funcionarioForm = this.fb.group({
    nome: ['', Validators.required],
    cargo: ['', Validators.required],
  });

  readonly pontoForm = this.fb.group({
    funcionarioId: ['', Validators.required],
    data: ['', Validators.required],
    horaEntrada: ['', Validators.required],
    horaSaida: [''],
  });

  constructor() {
    this.recarregarFuncionarios();
    this.recarregarRegistros();
  }

  recarregarFuncionarios(): void {
    this.rh.listarFuncionarios().subscribe(f => this.funcionarios.set(f));
  }

  recarregarRegistros(): void {
    const hoje = new Date().toISOString().slice(0, 10);
    const primeiro = this.funcionarios()[0]?.id;
    if (!primeiro) {
      return;
    }
    this.rh.listarPonto(primeiro, hoje, hoje).subscribe(registros => this.registros.set(registros));
  }

  cadastrarFuncionario(): void {
    const v = this.funcionarioForm.value;
    this.rh
      .registrarFuncionario({ nome: v.nome!, cargo: v.cargo! })
      .subscribe(() => {
        this.funcionarioForm.reset();
        this.recarregarFuncionarios();
      });
  }

  lancarPonto(): void {
    const v = this.pontoForm.value;
    this.rh
      .lancarPonto(v.funcionarioId!, {
        data: v.data!,
        horaEntrada: v.horaEntrada!,
        horaSaida: v.horaSaida || undefined,
      })
      .subscribe(() => {
        this.pontoForm.reset();
        this.recarregarRegistros();
      });
  }
}