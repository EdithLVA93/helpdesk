import { TicketPriority, TicketStatus } from './ticket.model';

/**
 * Textos en español para los valores que la API envía en inglés.
 * La API y la base de datos guardan los valores técnicos ("InProgress");
 * la interfaz muestra estas etiquetas.
 */
export const STATUS_LABELS: Readonly<Record<TicketStatus, string>> = {
  Open: 'Abierto',
  InProgress: 'En progreso',
  Resolved: 'Resuelto',
};

export const STATUS_ICONS: Readonly<Record<TicketStatus, string>> = {
  Open: 'radio_button_unchecked',
  InProgress: 'pending',
  Resolved: 'check_circle',
};

export const PRIORITY_LABELS: Readonly<Record<TicketPriority, string>> = {
  Low: 'Baja',
  Medium: 'Media',
  High: 'Alta',
  Critical: 'Crítica',
};

export const PRIORITY_ICONS: Readonly<Record<TicketPriority, string>> = {
  Low: 'keyboard_double_arrow_down',
  Medium: 'drag_handle',
  High: 'keyboard_arrow_up',
  Critical: 'keyboard_double_arrow_up',
};

export const STATUSES: readonly TicketStatus[] = ['Open', 'InProgress', 'Resolved'];

export const PRIORITIES: readonly TicketPriority[] = ['Low', 'Medium', 'High', 'Critical'];

/** Sugerencias del campo Categoría (se puede escribir cualquier otra). */
export const CATEGORY_SUGGESTIONS: readonly string[] = [
  'Access',
  'Hardware',
  'Infrastructure',
  'Network',
  'Security',
  'Software',
];
