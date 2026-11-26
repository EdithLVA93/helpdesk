/** Respuesta de error de la API (RFC 9457 ProblemDetails). */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  /** Solo en errores 400: mensajes por campo, con claves en camelCase ("additionalData.priority"). */
  errors?: Record<string, string[]>;
}

/**
 * Error HTTP normalizado. El interceptor convierte cualquier HttpErrorResponse
 * en un ApiError, así los componentes no dependen del formato del backend.
 */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly title: string,
    readonly detail: string,
    readonly fieldErrors: Readonly<Record<string, string[]>> = {},
  ) {
    super(detail);
    this.name = 'ApiError';
  }

  /** 0 = sin respuesta (API caída o sin red); 502-504 = el proxy no llegó a la API. */
  get isConnectionError(): boolean {
    return this.status === 0 || this.status === 502 || this.status === 503 || this.status === 504;
  }

  get isServerError(): boolean {
    return this.status >= 500 && !this.isConnectionError;
  }

  get hasFieldErrors(): boolean {
    return Object.keys(this.fieldErrors).length > 0;
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}
