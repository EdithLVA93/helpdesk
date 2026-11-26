import { Provider } from '@angular/core';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS, MatFormFieldDefaultOptions } from '@angular/material/form-field';

/**
 * Apariencia común de los campos de formulario. Se provee en cada pantalla
 * que tiene formularios (no en app.config) para que el código de Material
 * de formularios no entre en la carga inicial de la aplicación.
 */
export function provideFormFieldDefaults(): Provider {
  return {
    provide: MAT_FORM_FIELD_DEFAULT_OPTIONS,
    useValue: { appearance: 'outline', subscriptSizing: 'dynamic' } satisfies MatFormFieldDefaultOptions,
  };
}
