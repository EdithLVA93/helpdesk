import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { STATUS_ICONS, STATUS_LABELS } from '../../../features/tickets/models/ticket-labels';
import { TicketStatus } from '../../../features/tickets/models/ticket.model';

/** Etiqueta de color para el estado de un ticket. */
@Component({
  selector: 'app-status-badge',
  imports: [MatIconModule],
  template: `
    <span class="badge" [class]="'badge badge--status-' + status()">
      <mat-icon aria-hidden="true">{{ icon() }}</mat-icon>
      {{ label() }}
    </span>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadge {
  readonly status = input.required<TicketStatus>();

  protected readonly label = computed(() => STATUS_LABELS[this.status()]);
  protected readonly icon = computed(() => STATUS_ICONS[this.status()]);
}
