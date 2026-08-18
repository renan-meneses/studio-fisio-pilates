import { Component, input } from '@angular/core';

@Component({
  selector: 'clin-stat-card',
  standalone: true,
  template: `
    <div class="stat-card">
      <div class="stat-card__header">
        <span class="stat-card__label">{{ label() }}</span>
      </div>
      <div class="stat-card__value">{{ value() }}</div>
      <div class="stat-card__meta">{{ meta() }}</div>
    </div>
  `,
  styles: `
    .stat-card {
      background: var(--clin-surface);
      border: 1px solid var(--clin-border);
      border-radius: var(--clin-radius);
      box-shadow: var(--clin-shadow);
      padding: 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
    }
    .stat-card__label { font-size: 0.8rem; font-weight: 600; color: var(--clin-text-muted); text-transform: uppercase; letter-spacing: 0.05em; }
    .stat-card__value { font-size: 1.9rem; font-weight: 800; color: var(--clin-primary-dark); }
    .stat-card__meta { font-size: 0.85rem; color: var(--clin-text-muted); }
  `,
})
export class StatCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly meta = input<string>('');
}