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
    <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/60 p-4 backdrop-blur-sm animate-fade-in" (click)="fechado.emit()">
      <div
        class="w-full max-w-md max-h-[80vh] overflow-auto rounded-2xl border border-slate-200 bg-white p-5 shadow-pop dark:border-slate-800 dark:bg-slate-900 animate-scale-in"
        (click)="$event.stopPropagation()"
        role="dialog"
        aria-label="Selecione o horário"
      >
        <div class="mb-4 flex items-center justify-between gap-3">
          <span class="font-bold">Selecione o horário (24h)</span>
          <button class="btn btn--ghost btn--sm !px-2.5" type="button" (click)="fechado.emit()" aria-label="Fechar">
            ✕
          </button>
        </div>
        <div class="grid grid-cols-4 gap-1.5 sm:max-[480px]:grid-cols-3">
          @for (h of HORARIOS; track h) {
            <button
              type="button"
              class="cursor-pointer rounded-lg border px-1 py-2 font-mono text-[13px] tabular-nums transition-colors"
              [class]="
                h === valor()
                  ? 'border-teal-500 bg-teal-600 font-bold text-white'
                  : 'border-slate-200 bg-slate-50 text-slate-700 hover:border-teal-400 hover:bg-teal-50 hover:text-teal-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-teal-500/15 dark:hover:text-teal-300'
              "
              (click)="selecionado.emit(h)"
            >
              {{ h }}
            </button>
          }
        </div>
      </div>
    </div>
  `,
})
export class TimePickerModalComponent {
  readonly valor = input<string>('');
  readonly selecionado = output<string>();
  readonly fechado = output<void>();

  readonly HORARIOS = HORARIOS;
}
