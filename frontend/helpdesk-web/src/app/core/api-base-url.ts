import { InjectionToken } from '@angular/core';

import { environment } from '../../environments/environment';

/**
 * URL base de la API. Los servicios la inyectan en lugar de leer el
 * environment directamente, así en los tests se puede sustituir.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => environment.apiBaseUrl,
});
