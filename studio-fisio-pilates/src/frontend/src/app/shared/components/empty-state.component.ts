import { Component, input } from '@angular/core';

@Component({
  selector: 'clin-empty-state',
  standalone: true,
  template: `
    <div class="flex flex-col items-center gap-2 rounded-2xl px-4 py-12 text-center">
      <div
        class="flex size-14 items-center justify-center rounded-2xl bg-slate-100 text-2xl ring-1 ring-inset ring-slate-200 dark:bg-slate-800/60 dark:ring-slate-700"
      >
        {{ icone() }}
      </div>
      <p class="font-bold">{{ titulo() }}</p>
      @if (hint()) {
        <p class="max-w-sm text-sm text-slate-500 dark:text-slate-400">{{ hint() }}</p>
      }
    </div>
  `,
})
export class EmptyStateComponent {
  readonly icone = input('🗒️');
  readonly titulo = input.required<string>();
  readonly hint = input('');
}
