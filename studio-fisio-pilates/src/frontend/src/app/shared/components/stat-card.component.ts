import { Component, input } from '@angular/core';

@Component({
  selector: 'clin-stat-card',
  standalone: true,
  template: `
    <div
      class="flex flex-col gap-1 rounded-2xl border border-slate-200/80 bg-white p-5 shadow-card transition-shadow hover:shadow-lg dark:border-slate-800 dark:bg-slate-900"
    >
      <span class="text-[11px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        {{ label() }}
      </span>
      <div class="flex items-baseline gap-2">
        <span class="text-3xl font-extrabold tracking-tight text-teal-700 dark:text-teal-300">{{ value() }}</span>
        @if (meta()) {
          <span class="text-xs text-slate-400">{{ meta() }}</span>
        }
      </div>
    </div>
  `,
})
export class StatCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly meta = input<string>('');
}
