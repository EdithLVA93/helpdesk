import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { TicketService } from './ticket.service';

describe('TicketService', () => {
  let service: TicketService;
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(TicketService);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('envía solo los filtros con valor (sin "?status=" vacíos)', () => {
    service
      .getPaged({ status: 'Open', assignedUserId: null, priority: null, tag: '', sortDirection: 'Descending', page: 1 })
      .subscribe();

    const request = controller.expectOne((req) => req.url === '/api/tickets');

    expect(request.request.method).toBe('GET');
    expect(request.request.params.keys().sort()).toEqual(['page', 'sortDirection', 'status']);
    expect(request.request.params.get('status')).toBe('Open');
    request.flush({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 });
  });

  it('cambia el estado con PUT /api/tickets/{id}/status', () => {
    service.changeStatus(7, 'InProgress').subscribe();

    const request = controller.expectOne('/api/tickets/7/status');

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ status: 'InProgress' });
    request.flush({});
  });

  it('cambia la prioridad con PATCH /api/tickets/{id}/priority', () => {
    service.changePriority(7, 'Critical').subscribe();

    const request = controller.expectOne('/api/tickets/7/priority');

    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ priority: 'Critical' });
    request.flush({});
  });
});
