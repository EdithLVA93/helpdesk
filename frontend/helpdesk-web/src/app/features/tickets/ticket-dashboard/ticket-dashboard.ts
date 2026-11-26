import { BreakpointObserver } from '@angular/cdk/layout';
import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorIntl, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { catchError, finalize, map, Observable, of, Subject, switchMap, tap } from 'rxjs';

import { SpanishPaginatorIntl } from '../../../core/i18n/spanish-paginator-intl';
import { isApiError } from '../../../core/models/api-error.model';
import { PagedResult } from '../../../core/models/paged-result.model';
import { NotificationService } from '../../../core/services/notification.service';
import { PriorityBadge } from '../../../shared/components/priority-badge/priority-badge';
import { StatusBadge } from '../../../shared/components/status-badge/status-badge';
import { provideFormFieldDefaults } from '../../../shared/forms/form-field-defaults';
import { toIsoDate } from '../../../shared/utils/dates';
import { User } from '../../users/models/user.model';
import { UserService } from '../../users/services/user.service';
import {
  PRIORITIES,
  PRIORITY_ICONS,
  PRIORITY_LABELS,
  STATUS_ICONS,
  STATUS_LABELS,
  STATUSES,
} from '../models/ticket-labels';
import {
  SortDirection,
  TagUsage,
  Ticket,
  TicketPriority,
  TicketQuery,
  TicketStatus,
  TicketSummary,
} from '../models/ticket.model';
import { TicketService } from '../services/ticket.service';

interface KpiCard {
  label: string;
  icon: string;
  status: TicketStatus | null;
  value: (summary: TicketSummary) => number;
  tone: string;
}

type LoadOutcome = { result: PagedResult<Ticket> } | { error: string };

const DESKTOP_COLUMNS = ['id', 'title', 'assignedUserName', 'status', 'priority', 'createdAt', 'actions'];
const HANDSET_COLUMNS = ['title', 'status', 'actions'];

/** Dashboard de tickets: indicadores, filtros, listado paginado y cambios de estado/prioridad. */
@Component({
  selector: 'app-ticket-dashboard',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
    MatProgressBarModule,
    MatTooltipModule,
    StatusBadge,
    PriorityBadge,
  ],
  templateUrl: './ticket-dashboard.html',
  styleUrl: './ticket-dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [provideFormFieldDefaults(), { provide: MatPaginatorIntl, useClass: SpanishPaginatorIntl }],
})
export class TicketDashboard implements OnInit {
  private readonly ticketService = inject(TicketService);
  private readonly userService = inject(UserService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();

  protected readonly statuses = STATUSES;
  protected readonly priorities = PRIORITIES;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly statusIcons = STATUS_ICONS;
  protected readonly priorityLabels = PRIORITY_LABELS;
  protected readonly priorityIcons = PRIORITY_ICONS;
  protected readonly pageSizeOptions = [5, 10, 25, 50];
  protected readonly today = toIsoDate(new Date());

  protected readonly kpis: readonly KpiCard[] = [
    { label: 'Total', icon: 'confirmation_number', status: null, value: (s) => s.total, tone: 'total' },
    { label: 'Abiertos', icon: 'radio_button_unchecked', status: 'Open', value: (s) => s.open, tone: 'Open' },
    { label: 'En progreso', icon: 'pending', status: 'InProgress', value: (s) => s.inProgress, tone: 'InProgress' },
    { label: 'Resueltos', icon: 'check_circle', status: 'Resolved', value: (s) => s.resolved, tone: 'Resolved' },
  ];

  // ---- Estado de la pantalla (signals: compatibles con la detección de cambios zoneless) ----
  protected readonly page = signal<PagedResult<Ticket> | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly summary = signal<TicketSummary | null>(null);
  protected readonly users = signal<User[]>([]);
  protected readonly tags = signal<TagUsage[]>([]);
  protected readonly updatingId = signal<number | null>(null);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  protected readonly tickets = computed(() => this.page()?.items ?? []);
  protected readonly totalCount = computed(() => this.page()?.totalCount ?? 0);

  private readonly isHandset = toSignal(
    inject(BreakpointObserver)
      .observe('(max-width: 959.98px)')
      .pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  protected readonly displayedColumns = computed(() => (this.isHandset() ? HANDSET_COLUMNS : DESKTOP_COLUMNS));

  // ---- Filtros (formulario reactivo): cada cambio vuelve a consultar la API ----
  protected readonly filters = inject(NonNullableFormBuilder).group({
    status: [null as TicketStatus | null],
    assignedUserId: [null as number | null],
    priority: [null as TicketPriority | null],
    tag: [null as string | null],
    sortDirection: ['Descending' as SortDirection],
  });

  private readonly filterValue = toSignal(this.filters.valueChanges, { initialValue: this.filters.getRawValue() });

  protected readonly hasActiveFilters = computed(() => {
    const value = this.filterValue();

    return !!(value.status || value.assignedUserId || value.priority || value.tag);
  });

  protected readonly activeStatus = computed(() => this.filterValue().status ?? null);

  constructor() {
    // switchMap: si llega una consulta nueva antes de que termine la anterior,
    // la anterior se cancela y nunca se pinta un resultado obsoleto.
    this.reload$
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.loadError.set(null);
        }),
        switchMap(() => this.fetchTickets()),
        takeUntilDestroyed(),
      )
      .subscribe((outcome) => {
        this.loading.set(false);

        if ('result' in outcome) {
          this.page.set(outcome.result);
        } else {
          this.loadError.set(outcome.error);
        }
      });

    this.filters.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.pageIndex.set(0);
      this.reload$.next();
    });
  }

  ngOnInit(): void {
    this.reload$.next();
    this.loadSummary();
    this.loadTags();

    this.userService
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (users) => this.users.set(users), error: () => this.users.set([]) });
  }

  protected reload(): void {
    this.reload$.next();
  }

  protected filterByStatus(status: TicketStatus | null): void {
    this.filters.controls.status.setValue(status);
  }

  protected clearFilters(): void {
    this.filters.reset({ status: null, assignedUserId: null, priority: null, tag: null, sortDirection: 'Descending' });
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.reload$.next();
  }

  protected changeStatus(ticket: Ticket, status: TicketStatus): void {
    const from = STATUS_LABELS[ticket.status];

    this.runUpdate(ticket, this.ticketService.changeStatus(ticket.id, status), (updated) =>
      `Ticket #${updated.id}: ${from} → ${STATUS_LABELS[updated.status]}.`,
    );
  }

  protected changePriority(ticket: Ticket, priority: TicketPriority): void {
    this.runUpdate(ticket, this.ticketService.changePriority(ticket.id, priority), (updated) =>
      `Ticket #${updated.id}: prioridad ${PRIORITY_LABELS[priority].toLocaleLowerCase('es')}.`,
    );
  }

  protected isOverdue(ticket: Ticket): boolean {
    const date = ticket.additionalData.estimatedResolutionDate;

    return ticket.status !== 'Resolved' && date !== null && date < this.today;
  }

  /** Para el menú de acciones, cuyo contexto (let-ticket) no tiene tipo en la plantilla. */
  protected statusLabel(status: TicketStatus): string {
    return STATUS_LABELS[status];
  }

  protected kpiValue(kpi: KpiCard): number | null {
    const summary = this.summary();

    return summary ? kpi.value(summary) : null;
  }

  private runUpdate(ticket: Ticket, request: Observable<Ticket>, successMessage: (updated: Ticket) => string): void {
    this.updatingId.set(ticket.id);

    request
      .pipe(
        finalize(() => this.updatingId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updated) => {
          this.notifications.success(successMessage(updated));
          this.refreshAfterChange();
        },
        error: (error: unknown) => {
          // 409 (transición no permitida, p. ej. otro usuario ya cambió el ticket)
          // y 404: se muestra el motivo de la API y se refresca la vista.
          if (isApiError(error) && !error.isConnectionError && !error.isServerError) {
            this.notifications.error(error.detail);
            this.refreshAfterChange();
          }
        },
      });
  }

  /** Tras un cambio, el ticket puede dejar de cumplir el filtro y los contadores cambian. */
  private refreshAfterChange(): void {
    this.reload$.next();
    this.loadSummary();
  }

  private fetchTickets(): Observable<LoadOutcome> {
    const filters = this.filters.getRawValue();
    const query: TicketQuery = {
      ...filters,
      page: this.pageIndex() + 1,
      pageSize: this.pageSize(),
    };

    return this.ticketService.getPaged(query).pipe(
      map((result): LoadOutcome => ({ result })),
      catchError((error: unknown) =>
        of<LoadOutcome>({ error: isApiError(error) ? error.detail : 'No se pudieron cargar los tickets.' }),
      ),
    );
  }

  private loadSummary(): void {
    this.ticketService
      .getSummary()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (summary) => this.summary.set(summary), error: () => this.summary.set(null) });
  }

  private loadTags(): void {
    this.ticketService
      .getTags()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (tags) => this.tags.set(tags), error: () => this.tags.set([]) });
  }
}
