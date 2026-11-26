import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { PRIORITY_ICONS, PRIORITY_LABELS } from '../../../features/tickets/models/ticket-labels';
import { TicketPriority } from '../../../features/tickets/models/ticket.model';

/** Etiqueta de color para la prioridad (dato guardado en el JSON del ticket). */
@Component({
  selector: 'app-priority-badge',
  imports: [MatIconModule],
  template: `
    @if (priority(); as value) {
      <span [class]="'badge badge--priority-' + value">
        <mat-icon aria-hidden="true">{{ icons[value] }}</mat-icon>
        {{ labels[value] }}
      </span>
    } @else {
      <span class="badge badge--muted">Sin prioridad</span>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PriorityBadge {
  readonly priority = input<TicketPriority | null>(null);

  protected readonly labels = PRIORITY_LABELS;
  protected readonly icons = PRIORITY_ICONS;
}
