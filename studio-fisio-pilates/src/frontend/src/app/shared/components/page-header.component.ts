import { Component, input } from '@angular/core';

@Component({
  selector: 'clin-page-header',
  standalone: true,
  template: `
    <div class="mb-6 flex items-start justify-between gap-4">
      <div>
        <h1 class="text-2xl font-extrabold tracking-tight">{{ titulo() }}</h1>
        @if (subtitulo()) {
          <p class="mt-0.5 text-sm text-slate-500 dark:text-slate-400">{{ subtitulo() }}</p>
        }
      </div>
      <div class="flex shrink-0 flex-wrap items-center gap-2">
        <ng-content />
      </div>
    </div>
  `,
})
export class PageHeaderComponent {
  readonly titulo = input.required<string>();
  readonly subtitulo = input<string>();
}
