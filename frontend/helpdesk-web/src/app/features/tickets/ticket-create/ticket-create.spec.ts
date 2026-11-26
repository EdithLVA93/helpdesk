import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { ApiError } from '../../../core/models/api-error.model';
import { NotificationService } from '../../../core/services/notification.service';
import { toIsoDate } from '../../../shared/utils/dates';
import { UserService } from '../../users/services/user.service';
import { Ticket } from '../models/ticket.model';
import { TicketService } from '../services/ticket.service';
import { TicketCreate } from './ticket-create';

describe('TicketCreate', () => {
  const ticketService = { create: vi.fn() };
  const userService = {
    getAll: vi.fn(() =>
      of([{ id: 3, name: 'Lucía Ramírez', email: 'lucia.ramirez@example.com', createdAt: '2026-11-01T00:00:00Z' }]),
    ),
  };
  const notifications = { success: vi.fn(), error: vi.fn() };

  let fixture: ComponentFixture<TicketCreate>;
  let component: TicketCreate;
  let router: Router;

  beforeEach(async () => {
    vi.clearAllMocks();

    await TestBed.configureTestingModule({
      imports: [TicketCreate],
      providers: [
        provideRouter([]),
        { provide: TicketService, useValue: ticketService },
        { provide: UserService, useValue: userService },
        { provide: NotificationService, useValue: notifications },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TicketCreate);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    await fixture.whenStable();
  });

  function text(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  /** Fecha válida para el datepicker (mínimo: hoy), a 10 días vista. */
  function inTenDays(): Date {
    const date = new Date();
    date.setDate(date.getDate() + 10);
    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
  }

  it('no envía el formulario vacío y muestra los mensajes de los campos obligatorios', async () => {
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await fixture.whenStable();

    expect(ticketService.create).not.toHaveBeenCalled();
    expect(text()).toContain('El título del ticket es obligatorio.');
    expect(text()).toContain('Todo ticket debe tener un usuario asignado.');
    expect(text()).toContain('Seleccione una prioridad.');
    expect(text()).toContain('La categoría es obligatoria.');
    expect(notifications.error).toHaveBeenCalledWith('Revise los campos marcados antes de crear el ticket.');
  });

  it('rechaza un título con solo espacios', async () => {
    component['form'].controls.title.setValue('   ');

    expect(component['form'].controls.title.hasError('required')).toBe(true);
  });

  it('envía la petición con el formato de la API y vuelve al listado', async () => {
    const date = inTenDays();
    ticketService.create.mockReturnValue(of({ id: 30, assignedUserName: 'Lucía Ramírez' } as Ticket));

    component['form'].setValue({
      title: '  VPN caída  ',
      description: '   ',
      assignedUserId: 3,
      additionalData: {
        priority: 'High',
        category: ' Network ',
        estimatedResolutionDate: date,
        tags: ['vpn', 'remote'],
        department: ' Ventas ',
      },
    });
    component['submit']();
    await fixture.whenStable();

    expect(ticketService.create).toHaveBeenCalledWith({
      title: 'VPN caída',
      description: null,
      assignedUserId: 3,
      additionalData: {
        priority: 'High',
        category: 'Network',
        estimatedResolutionDate: toIsoDate(date),
        tags: ['vpn', 'remote'],
        metadata: { department: 'Ventas', source: 'Web' },
      },
    });
    expect(notifications.success).toHaveBeenCalledWith('Ticket #30 creado y asignado a Lucía Ramírez.');
    expect(router.navigate).toHaveBeenCalledWith(['/tickets']);
  });

  it('muestra en el campo correspondiente los errores 400 de la API', async () => {
    ticketService.create.mockReturnValue(
      throwError(
        () =>
          new ApiError(400, 'Uno o más campos no son válidos.', 'Uno o más campos no son válidos.', {
            assignedUserId: ["El usuario asignado con id '3' no existe."],
          }),
      ),
    );

    component['form'].patchValue({
      title: 'VPN caída',
      assignedUserId: 3,
      additionalData: { priority: 'Low', category: 'Network' },
    });
    component['submit']();
    await fixture.whenStable();

    expect(component['form'].controls.assignedUserId.errors?.['server']).toBe("El usuario asignado con id '3' no existe.");
    expect(text()).toContain("El usuario asignado con id '3' no existe.");
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
