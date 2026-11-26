import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormGroupDirective, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';

import { isApiError } from '../../../core/models/api-error.model';
import { NotificationService } from '../../../core/services/notification.service';
import { provideFormFieldDefaults } from '../../../shared/forms/form-field-defaults';
import { emailFormat, notBlank } from '../../../shared/forms/form-validators';
import { applyServerErrors, serverError } from '../../../shared/forms/server-errors';
import { User, USER_LIMITS } from '../models/user.model';
import { UserService } from '../services/user.service';

/** Gestión de usuarios: alta y listado. */
@Component({
  selector: 'app-user-management',
  imports: [
    ReactiveFormsModule,
    DatePipe,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './user-management.html',
  styleUrl: './user-management.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [provideFormFieldDefaults()],
})
export class UserManagement implements OnInit {
  private readonly userService = inject(UserService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formDirective = viewChild.required(FormGroupDirective);

  protected readonly limits = USER_LIMITS;
  protected readonly serverError = serverError;

  protected readonly users = signal<User[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly search = signal('');

  /** Búsqueda local por nombre o correo (la lista completa ya está cargada). */
  protected readonly filteredUsers = computed(() => {
    const term = this.search().trim().toLocaleLowerCase('es');

    return term.length === 0
      ? this.users()
      : this.users().filter(
          (user) =>
            user.name.toLocaleLowerCase('es').includes(term) || user.email.toLocaleLowerCase('es').includes(term),
        );
  });

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, notBlank, Validators.maxLength(USER_LIMITS.nameMaxLength)]],
    email: ['', [Validators.required, notBlank, emailFormat, Validators.maxLength(USER_LIMITS.emailMaxLength)]],
  });

  ngOnInit(): void {
    this.loadUsers();
  }

  protected loadUsers(): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.userService
      .getAll()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (users) => this.users.set(users),
        error: (error: unknown) =>
          this.loadError.set(isApiError(error) ? error.detail : 'No se pudieron cargar los usuarios.'),
      });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, email } = this.form.getRawValue();
    this.saving.set(true);

    this.userService
      .create({ name: name.trim(), email: email.trim() })
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (user) => {
          this.users.update((users) => [...users, user].sort((a, b) => a.name.localeCompare(b.name, 'es')));
          this.notifications.success(`Usuario «${user.name}» creado correctamente.`);
          // resetForm (de la directiva) también limpia el estado "enviado" y los errores visibles.
          this.formDirective().resetForm();
        },
        error: (error: unknown) => this.handleSaveError(error),
      });
  }

  protected initials(name: string): string {
    return name
      .split(/\s+/)
      .filter((part) => part.length > 0)
      .slice(0, 2)
      .map((part) => part[0].toLocaleUpperCase('es'))
      .join('');
  }

  private handleSaveError(error: unknown): void {
    // Sin conexión y errores 5xx ya los notificó el interceptor.
    if (!isApiError(error) || error.isConnectionError || error.isServerError) {
      return;
    }

    // 409: correo ya registrado → se marca el campo.
    if (error.status === 409) {
      this.form.controls.email.setErrors({ server: error.detail });
      this.form.controls.email.markAsTouched();
      return;
    }

    if (error.hasFieldErrors) {
      const unmatched = applyServerErrors(this.form, error.fieldErrors);

      if (unmatched.length > 0) {
        this.notifications.error(unmatched.join(' '));
      }

      return;
    }

    this.notifications.error(error.detail);
  }
}
