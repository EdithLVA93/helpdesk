import { COMMA, ENTER } from '@angular/cdk/keycodes';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatChipInputEvent, MatChipsModule } from '@angular/material/chips';
import { DateAdapter, MAT_DATE_LOCALE, provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerIntl, MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { DayMonthYearDateAdapter } from '../../../core/i18n/day-month-year-date-adapter';
import { SpanishDatepickerIntl } from '../../../core/i18n/spanish-datepicker-intl';
import { isApiError } from '../../../core/models/api-error.model';
import { NotificationService } from '../../../core/services/notification.service';
import { provideFormFieldDefaults } from '../../../shared/forms/form-field-defaults';
import { notBlank } from '../../../shared/forms/form-validators';
import { applyServerErrors, serverError } from '../../../shared/forms/server-errors';
import { startOfToday, toIsoDate } from '../../../shared/utils/dates';
import { User } from '../../users/models/user.model';
import { UserService } from '../../users/services/user.service';
import { CATEGORY_SUGGESTIONS, PRIORITIES, PRIORITY_ICONS, PRIORITY_LABELS } from '../models/ticket-labels';
import { CreateTicketRequest, TICKET_LIMITS, TicketPriority } from '../models/ticket.model';
import { TicketService } from '../services/ticket.service';

/** Origen que se guarda en metadata.source para los tickets creados desde esta aplicación. */
const TICKET_SOURCE = 'Web';

/** Máximo de elementos en un arreglo (etiquetas). */
function maxItems(max: number) {
  return (control: AbstractControl<string[]>): ValidationErrors | null =>
    control.value.length > max ? { maxItems: { max } } : null;
}

/** Formulario reactivo para crear un ticket. */
@Component({
  selector: 'app-ticket-create',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatAutocompleteModule,
    MatDatepickerModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './ticket-create.html',
  styleUrl: './ticket-create.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    provideFormFieldDefaults(),
    provideNativeDateAdapter(),
    // Después de provideNativeDateAdapter: sustituye su adaptador para aceptar "dd/mm/aaaa".
    { provide: DateAdapter, useClass: DayMonthYearDateAdapter },
    { provide: MAT_DATE_LOCALE, useValue: 'es-CO' },
    { provide: MatDatepickerIntl, useClass: SpanishDatepickerIntl },
  ],
})
export class TicketCreate implements OnInit {
  private readonly ticketService = inject(TicketService);
  private readonly userService = inject(UserService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly limits = TICKET_LIMITS;
  protected readonly priorities = PRIORITIES;
  protected readonly priorityLabels = PRIORITY_LABELS;
  protected readonly priorityIcons = PRIORITY_ICONS;
  protected readonly separatorKeys = [ENTER, COMMA] as const;
  protected readonly minDate = startOfToday();
  protected readonly serverError = serverError;

  protected readonly users = signal<User[]>([]);
  protected readonly usersLoading = signal(true);
  protected readonly usersError = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly tagMessage = signal<string | null>(null);

  /** Misma forma que CreateTicketRequest: los errores "additionalData.x" de la API encajan con form.get(). */
  protected readonly form = this.fb.group({
    title: ['', [Validators.required, notBlank, Validators.maxLength(TICKET_LIMITS.titleMaxLength)]],
    description: ['', Validators.maxLength(TICKET_LIMITS.descriptionMaxLength)],
    assignedUserId: this.fb.control<number | null>(null, Validators.required),
    additionalData: this.fb.group({
      priority: this.fb.control<TicketPriority | null>(null, Validators.required),
      category: ['', [Validators.required, notBlank, Validators.maxLength(TICKET_LIMITS.categoryMaxLength)]],
      estimatedResolutionDate: this.fb.control<Date | null>(null),
      tags: this.fb.control<string[]>([], maxItems(TICKET_LIMITS.maxTags)),
      department: ['', Validators.maxLength(200)],
    }),
  });

  protected readonly controls = this.form.controls;
  protected readonly data = this.form.controls.additionalData.controls;

  private readonly categoryValue = toSignal(this.data.category.valueChanges, { initialValue: '' });

  /** Sugerencias de categoría filtradas por lo que se va escribiendo. */
  protected readonly categoryOptions = computed(() => {
    const term = this.categoryValue().trim().toLocaleLowerCase('es');

    return CATEGORY_SUGGESTIONS.filter((category) => category.toLocaleLowerCase('es').includes(term));
  });

  ngOnInit(): void {
    this.loadUsers();
  }

  protected loadUsers(): void {
    this.usersLoading.set(true);
    this.usersError.set(null);

    this.userService
      .getAll()
      .pipe(
        finalize(() => this.usersLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (users) => this.users.set(users),
        error: (error: unknown) =>
          this.usersError.set(isApiError(error) ? error.detail : 'No se pudieron cargar los usuarios.'),
      });
  }

  protected addTag(event: MatChipInputEvent): void {
    const tag = event.value.trim().toLocaleLowerCase('es');
    const tags = this.data.tags.value;

    if (tag.length === 0) {
      return;
    }

    if (tag.length > TICKET_LIMITS.tagMaxLength) {
      this.tagMessage.set(`Cada etiqueta puede tener como máximo ${TICKET_LIMITS.tagMaxLength} caracteres.`);
      return;
    }

    if (tags.length >= TICKET_LIMITS.maxTags) {
      this.tagMessage.set(`Se permiten como máximo ${TICKET_LIMITS.maxTags} etiquetas.`);
      return;
    }

    if (!tags.includes(tag)) {
      this.data.tags.setValue([...tags, tag]);
    }

    this.tagMessage.set(null);
    event.chipInput.clear();
  }

  protected removeTag(tag: string): void {
    this.data.tags.setValue(this.data.tags.value.filter((value) => value !== tag));
    this.tagMessage.set(null);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifications.error('Revise los campos marcados antes de crear el ticket.');
      return;
    }

    this.saving.set(true);

    this.ticketService
      .create(this.buildRequest())
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (ticket) => {
          this.notifications.success(`Ticket #${ticket.id} creado y asignado a ${ticket.assignedUserName}.`);
          void this.router.navigate(['/tickets']);
        },
        error: (error: unknown) => this.handleSaveError(error),
      });
  }

  private buildRequest(): CreateTicketRequest {
    const value = this.form.getRawValue();
    const data = value.additionalData;
    const department = data.department.trim();

    return {
      title: value.title.trim(),
      description: value.description.trim() || null,
      assignedUserId: value.assignedUserId!,
      additionalData: {
        priority: data.priority!,
        category: data.category.trim(),
        estimatedResolutionDate: data.estimatedResolutionDate ? toIsoDate(data.estimatedResolutionDate) : null,
        tags: data.tags,
        metadata: department ? { department, source: TICKET_SOURCE } : { source: TICKET_SOURCE },
      },
    };
  }

  private handleSaveError(error: unknown): void {
    // Sin conexión y errores 5xx ya los notificó el interceptor.
    if (!isApiError(error) || error.isConnectionError || error.isServerError) {
      return;
    }

    if (error.hasFieldErrors) {
      const unmatched = applyServerErrors(this.form, error.fieldErrors);
      this.notifications.error(unmatched.length > 0 ? unmatched.join(' ') : 'Revise los campos marcados.');
      return;
    }

    this.notifications.error(error.detail);
  }
}
