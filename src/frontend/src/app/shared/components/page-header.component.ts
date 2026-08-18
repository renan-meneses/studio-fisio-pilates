import { Component, input } from '@angular/core';

@Component({
  selector: 'clin-page-header',
  standalone: true,
  template: `
    <div class="page-header">
      <div>
        <h1 class="page-header__title">{{ titulo() }}</h1>
        @if (subtitulo()) {
          <p class="page-header__subtitle">{{ subtitulo() }}</p>
        }
      </div>
      <div class="page-header__actions">
        <ng-content />
      </div>
    </div>
  `,
  styles: `
    .page-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1.25rem;
    }
    .page-header__title { font-size: 1.4rem; }
    .page-header__subtitle { margin: 0.3rem 0 0; color: var(--clin-text-muted); }
    .page-header__actions { display: flex; gap: 0.6rem; }
  `,
})
export class PageHeaderComponent {
  readonly titulo = input.required<string>();
  readonly subtitulo = input<string>();
}