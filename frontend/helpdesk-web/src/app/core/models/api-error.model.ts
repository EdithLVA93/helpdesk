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
  /**
   * @param isConnectionError true cuando la respuesta no vino de la API: sin red,
   *   API caída o un proxy/gateway que no pudo llegar a ella (lo decide el interceptor).
   */
  constructor(
    readonly status: number,
    readonly title: string,
    readonly detail: string,
    readonly fieldErrors: Readonly<Record<string, string[]>> = {},
    readonly isConnectionError = false,
  ) {
    super(detail);
    this.name = 'ApiError';
  }

  /** Error interno de la API (5xx con ProblemDetails). */
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
