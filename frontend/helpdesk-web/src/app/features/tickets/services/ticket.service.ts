import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../../../core/api-base-url';
import { PagedResult } from '../../../core/models/paged-result.model';
import {
  CreateTicketRequest,
  TagUsage,
  Ticket,
  TicketPriority,
  TicketQuery,
  TicketStatus,
  TicketSummary,
} from '../models/ticket.model';

/** Comunicación con /api/tickets. Los errores llegan como ApiError (interceptor). */
@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/tickets`;

  /** GET /api/tickets: filtros, orden y paginación se resuelven en el servidor. */
  getPaged(query: TicketQuery): Observable<PagedResult<Ticket>> {
    return this.http.get<PagedResult<Ticket>>(this.baseUrl, { params: toHttpParams(query) });
  }

  getById(id: number): Observable<Ticket> {
    return this.http.get<Ticket>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateTicketRequest): Observable<Ticket> {
    return this.http.post<Ticket>(this.baseUrl, request);
  }

  /** PUT /api/tickets/{id}/status. Una transición no permitida responde 409. */
  changeStatus(id: number, status: TicketStatus): Observable<Ticket> {
    return this.http.put<Ticket>(`${this.baseUrl}/${id}/status`, { status });
  }

  /** PATCH /api/tickets/{id}/priority (JSON_MODIFY en SQL Server). */
  changePriority(id: number, priority: TicketPriority): Observable<Ticket> {
    return this.http.patch<Ticket>(`${this.baseUrl}/${id}/priority`, { priority });
  }

  getSummary(): Observable<TicketSummary> {
    return this.http.get<TicketSummary>(`${this.baseUrl}/summary`);
  }

  getTags(): Observable<TagUsage[]> {
    return this.http.get<TagUsage[]>(`${this.baseUrl}/tags`);
  }
}

/** Solo se envían los filtros con valor, para no mandar "?status=" vacíos. */
function toHttpParams(query: TicketQuery): HttpParams {
  let params = new HttpParams();

  for (const [key, value] of Object.entries(query)) {
    if (value !== null && value !== undefined && value !== '') {
      params = params.set(key, String(value));
    }
  }

  return params;
}
