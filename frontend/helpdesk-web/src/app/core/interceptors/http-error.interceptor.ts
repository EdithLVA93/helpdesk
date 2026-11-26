import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { ApiError, ProblemDetails } from '../models/api-error.model';
import { NotificationService } from '../services/notification.service';

/**
 * Manejo centralizado de errores HTTP:
 *  - convierte HttpErrorResponse en ApiError (con el ProblemDetails de la API);
 *  - notifica directamente los errores que el usuario no puede corregir
 *    (sin conexión, error del servidor);
 *  - deja los 4xx (validación, no encontrado, conflicto) para el componente,
 *    que conoce el contexto: marcar un campo, mostrar la regla incumplida...
 */
export const httpErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const notifications = inject(NotificationService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      const apiError = toApiError(error);

      if (apiError.isConnectionError || apiError.isServerError) {
        notifications.error(apiError.detail);
      }

      return throwError(() => apiError);
    }),
  );
};

function toApiError(error: HttpErrorResponse): ApiError {
  const problem = isProblemDetails(error.error) ? error.error : undefined;
  const status = error.status;

  if (status === 0 || status === 502 || status === 503 || status === 504) {
    return new ApiError(
      status,
      'Sin conexión con la API',
      'No se pudo conectar con el servidor. Verifique que la API esté en ejecución.',
    );
  }

  return new ApiError(
    status,
    problem?.title ?? 'Error',
    problem?.detail ?? problem?.title ?? defaultMessage(status),
    problem?.errors ?? {},
  );
}

function isProblemDetails(body: unknown): body is ProblemDetails {
  return typeof body === 'object' && body !== null && ('title' in body || 'detail' in body || 'errors' in body);
}

function defaultMessage(status: number): string {
  switch (status) {
    case 400:
      return 'La solicitud no es válida.';
    case 404:
      return 'El recurso solicitado no existe.';
    case 409:
      return 'La operación entra en conflicto con el estado actual de los datos.';
    default:
      return status >= 500
        ? 'Ocurrió un error inesperado en el servidor. Intente nuevamente más tarde.'
        : `Error inesperado (${status}).`;
  }
}
