import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import localeEsCo from '@angular/common/locales/es-CO';
import {
  ApplicationConfig,
  inject,
  LOCALE_ID,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { MatIconRegistry } from '@angular/material/icon';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { routes } from './app.routes';
import { httpErrorInterceptor } from './core/interceptors/http-error.interceptor';

// Fechas, números y nombres de meses en español (Colombia).
registerLocaleData(localeEsCo);

/**
 * Configuración global. Los proveedores que solo usa una pantalla (paginador
 * en español, adaptador de fechas, apariencia de los formularios) se declaran
 * en esa pantalla: así su código se descarga con ella y no en el arranque.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),

    // HttpClient con fetch y un interceptor que convierte cualquier error HTTP
    // en un ApiError tipado (ProblemDetails de la API).
    provideHttpClient(withFetch(), withInterceptors([httpErrorInterceptor])),

    { provide: LOCALE_ID, useValue: 'es-CO' },

    // <mat-icon> usa la fuente "Material Symbols Outlined" (index.html).
    provideAppInitializer(() => {
      inject(MatIconRegistry).setDefaultFontSetClass('material-symbols-outlined');
    }),
  ],
};
