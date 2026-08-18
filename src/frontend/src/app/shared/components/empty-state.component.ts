import { Component, input } from '@angular/core';

@Component({
  selector: 'clin-empty-state',
  standalone: true,
  template: `
    <div class="empty-state">
      <div class="empty-state__icon">{{ icone() }}</div>
      <p class="empty-state__title">{{ titulo() }}</p>
      <p class="empty-state__hint">{{ hint() }}</p>
    </div>
  `,
  styles: `
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      gap: 0.5rem;
      padding: 3rem 1rem;
      color: var(--clin-text-muted);
    }
    .empty-state__icon { font-size: 2.4rem; }
    .empty-state__title { font-weight: 700; color: var(--clin-text); }
    .empty-state__hint { font-size: 0.85rem; }
  `,
})
export class EmptyStateComponent {
  readonly icone = input('🗒️');
  readonly titulo = input.required<string>();
  readonly hint = input('');
}