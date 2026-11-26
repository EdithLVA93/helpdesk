/**
 * Fecha local en formato "yyyy-MM-dd" (la que espera la API para
 * estimatedResolutionDate).
 *
 * No se usa toISOString(): convierte a UTC y, en Colombia (UTC-5), una fecha
 * elegida después de las 19:00 se enviaría como el día siguiente.
 */
export function toIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

/** Hoy a las 00:00 (hora local). */
export function startOfToday(): Date {
  const now = new Date();

  return new Date(now.getFullYear(), now.getMonth(), now.getDate());
}
