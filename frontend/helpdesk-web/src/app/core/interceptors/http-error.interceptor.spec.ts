import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { ApiError } from '../models/api-error.model';
import { NotificationService } from '../services/notification.service';
import { httpErrorInterceptor } from './http-error.interceptor';

describe('httpErrorInterceptor', () => {
  const notifications = { error: vi.fn(), success: vi.fn() };
  let http: HttpClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    notifications.error.mockReset();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: NotificationService, useValue: notifications },
      ],
    });

    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  /** Lanza GET /api/test y devuelve (como promesa) el error que llegaría al componente. */
  function captureError(): Promise<ApiError> {
    return firstValueFrom(http.get('/api/test')).then(
      () => {
        throw new Error('La petición debía fallar');
      },
      (error: unknown) => error as ApiError,
    );
  }

  /** Responde a la petición con el estado y cuerpo indicados. */
  async function failWith(status: number, body: object | null): Promise<ApiError> {
    const result = captureError();

    controller.expectOne('/api/test').flush(body, { status, statusText: 'Error' });

    return result;
  }

  it('convierte un 409 de la API en ApiError con el mensaje de la regla, sin notificar', async () => {
    const error = await failWith(409, {
      title: 'Regla de negocio incumplida',
      status: 409,
      detail: "No se permite cambiar el estado de 'Open' a 'Resolved'.",
    });

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(409);
    expect(error.detail).toBe("No se permite cambiar el estado de 'Open' a 'Resolved'.");
    expect(error.isConnectionError).toBe(false);
    // Los 4xx los maneja el componente, que conoce el contexto.
    expect(notifications.error).not.toHaveBeenCalled();
  });

  it('conserva los errores por campo de un 400', async () => {
    const error = await failWith(400, {
      title: 'Uno o más campos no son válidos.',
      status: 400,
      errors: { title: ['El título del ticket es obligatorio.'] },
    });

    expect(error.hasFieldErrors).toBe(true);
    expect(error.fieldErrors['title']).toEqual(['El título del ticket es obligatorio.']);
    expect(notifications.error).not.toHaveBeenCalled();
  });

  it('usa un mensaje por defecto si un 404 llega sin cuerpo', async () => {
    const error = await failWith(404, null);

    expect(error.detail).toBe('El recurso solicitado no existe.');
  });

  it('notifica un 500 de la API (con ProblemDetails) como error del servidor', async () => {
    const error = await failWith(500, {
      title: 'Error interno del servidor',
      status: 500,
      detail: 'Ocurrió un error inesperado. Intente nuevamente más tarde.',
    });

    expect(error.isServerError).toBe(true);
    expect(error.isConnectionError).toBe(false);
    expect(notifications.error).toHaveBeenCalledWith('Ocurrió un error inesperado. Intente nuevamente más tarde.');
  });

  it('trata un 500 sin ProblemDetails (proxy sin API) como falta de conexión', async () => {
    const error = await failWith(500, null);

    expect(error.isConnectionError).toBe(true);
    expect(error.isServerError).toBe(false);
    expect(notifications.error).toHaveBeenCalledWith(
      'No se pudo conectar con el servidor. Verifique que la API esté en ejecución.',
    );
  });

  it('trata un error de red (estado 0) como falta de conexión', async () => {
    const result = captureError();

    controller.expectOne('/api/test').error(new ProgressEvent('error'));
    const error = await result;

    expect(error.status).toBe(0);
    expect(error.isConnectionError).toBe(true);
    expect(notifications.error).toHaveBeenCalledTimes(1);
  });
});
