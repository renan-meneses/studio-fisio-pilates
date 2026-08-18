import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { ProntuarioService } from '../services/prontuario.service';
import { PacienteResumo } from '../models/prontuario.model';

@Component({
  selector: 'clin-pacientes-page',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent, RouterLink],
  template: `
    <clin-page-header
      titulo="Prontuários"
      subtitulo="Pacientes cadastrados e seus prontuários eletrônicos"
    />

    <div class="card">
      <div class="pacientes__busca">
        <input
          class="pacientes__input"
          placeholder="Buscar paciente…"
          (input)="buscar($event)"
        />
      </div>

      @if (pacientes().length === 0) {
        <clin-empty-state
          icone="🧑‍⚕️"
          titulo="Nenhum paciente encontrado"
          hint="Ajuste o termo de busca ou adicione pacientes pelo cadastro."
        />
      } @else {
        <table class="data-table">
          <thead>
            <tr>
              <th>Paciente</th>
              <th>Telefone</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (pac of pacientes(); track pac.id) {
              <tr>
                <td>{{ pac.nome }}</td>
                <td>{{ pac.telefone ?? '—' }}</td>
                <td>
                  <span class="badge {{ pac.ativo ? 'badge--success' : 'badge--danger' }}">
                    {{ pac.ativo ? 'Ativo' : 'Inativo' }}
                  </span>
                </td>
                <td>
                  <a class="btn btn--outline" [routerLink]="['/prontuarios', pac.id]">
                    Abrir prontuário
                  </a>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `,
  styles: `
    .pacientes__busca { margin-bottom: 1rem; }
    .pacientes__input {
      width: 100%;
      max-width: 360px;
      padding: 0.6rem 0.75rem;
      border: 1px solid var(--clin-border);
      border-radius: 8px;
      font: inherit;
    }
  `,
})
export class PacientesPageComponent {
  private readonly prontuario = inject(ProntuarioService);

  readonly pacientes = signal<PacienteResumo[]>([]);

  constructor() {
    this.recarregar();
  }

  recarregar(termo?: string): void {
    this.prontuario.listarPacientes(termo).subscribe({
      next: lista => this.pacientes.set(lista),
      error: () => alert('Falha ao carregar pacientes.'),
    });
  }

  buscar(evento: Event): void {
    const termo = (evento.target as HTMLInputElement).value;
    this.recarregar(termo || undefined);
  }
}