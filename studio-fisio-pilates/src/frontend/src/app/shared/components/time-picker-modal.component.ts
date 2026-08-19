import { Component, input, output } from '@angular/core';

const HORARIOS: string[] = Array.from({ length: 48 }, (_, i) => {
  const h = Math.floor(i / 2).toString().padStart(2, '0');
  const m = i % 2 === 0 ? '00' : '30';
  return `${h}:${m}`;
});

@Component({
  selector: 'clin-time-picker-modal',
  standalone: true,
  template: `
    <div class="time-picker__overlay" (click)="fechado.emit()">
      <div class="time-picker" (click)="$event.stopPropagation()">
        <div class="time-picker__header">
          <span>Selecione o horário (24h)</span>
          <button class="btn btn--outline" type="button" (click)="fechado.emit()">✕</button>
        </div>
        <div class="time-picker__grid">
          @for (h of HORARIOS; track h) {
            <button
              type="button"
              class="time-picker__slot"
              [class.time-picker__slot--ativo]="h === valor()"
              (click)="selecionado.emit(h)"
            >
              {{ h }}
            </button>
          }
        </div>
      </div>
    </div>
  `,
  styles: `
    .time-picker__overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.55);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 1rem;
    }
    .time-picker {
      background: var(--clin-surface);
      border: 1px solid var(--clin-border);
      border-radius: 14px;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.35);
      padding: 1.25rem;
      width: 100%;
      max-width: 460px;
      max-height: 80vh;
      overflow: auto;
    }
    .time-picker__header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1rem;
      font-weight: 700;
      color: var(--clin-text);
    }
    .time-picker__grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 0.4rem;
    }
    .time-picker__slot {
      padding: 0.5rem;
      border: 1px solid var(--clin-border);
      border-radius: 8px;
      background: var(--clin-surface-alt, #f8fafc);
      color: var(--clin-text);
      font: inherit;
      font-size: 0.85rem;
      font-variant-numeric: tabular-nums;
      cursor: pointer;
      transition: background 0.12s ease, color 0.12s ease;

      &:hover { background: var(--clin-primary); color: #fff; }
      &.time-picker__slot--ativo {
        background: var(--clin-primary);
        border-color: var(--clin-primary);
        color: #fff;
        font-weight: 700;
      }
    }
    @media (max-width: 480px) {
      .time-picker__grid { grid-template-columns: repeat(3, 1fr); }
    }
  `,
})
export class TimePickerModalComponent {
  readonly valor = input<string>('');
  readonly selecionado = output<string>();
  readonly fechado = output<void>();

  readonly HORARIOS = HORARIOS;
}
