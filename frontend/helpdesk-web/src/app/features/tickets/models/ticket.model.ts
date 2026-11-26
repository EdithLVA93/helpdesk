/** Tipos que reflejan los DTOs de la API de tickets. */

export type TicketStatus = 'Open' | 'InProgress' | 'Resolved';

export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical';

export type SortDirection = 'Descending' | 'Ascending';

/** Información adicional guardada como JSON en Tickets.AdditionalData. */
export interface TicketAdditionalData {
  priority: TicketPriority | null;
  category: string | null;
  /** Fecha sin hora: "yyyy-MM-dd". */
  estimatedResolutionDate: string | null;
  tags: string[];
  metadata: Record<string, string>;
}

/** TicketResponse de la API. */
export interface Ticket {
  id: number;
  title: string;
  description: string | null;
  status: TicketStatus;
  assignedUserId: number;
  assignedUserName: string;
  /** ISO 8601 en UTC. */
  createdAt: string;
  updatedAt: string | null;
  additionalData: TicketAdditionalData;
  /** Estados a los que puede pasar el ticket; la API es quien decide. */
  allowedTransitions: TicketStatus[];
}

/** CreateTicketRequest de la API (POST /api/tickets). */
export interface CreateTicketRequest {
  title: string;
  description: string | null;
  assignedUserId: number;
  additionalData: {
    priority: TicketPriority;
    category: string;
    estimatedResolutionDate: string | null;
    tags: string[];
    metadata: Record<string, string>;
  };
}

/** Parámetros de GET /api/tickets. Todos opcionales y combinables. */
export interface TicketQuery {
  status?: TicketStatus | null;
  assignedUserId?: number | null;
  priority?: TicketPriority | null;
  category?: string | null;
  tag?: string | null;
  sortDirection?: SortDirection;
  page?: number;
  pageSize?: number;
}

/** TicketSummaryResponse de la API. */
export interface TicketSummary {
  total: number;
  open: number;
  inProgress: number;
  resolved: number;
}

/** TagUsageResponse de la API. */
export interface TagUsage {
  tag: string;
  ticketCount: number;
}

/** Límites del backend (constantes de la entidad Ticket). */
export const TICKET_LIMITS = {
  titleMaxLength: 150,
  descriptionMaxLength: 2000,
  categoryMaxLength: 50,
  maxTags: 10,
  tagMaxLength: 30,
} as const;
