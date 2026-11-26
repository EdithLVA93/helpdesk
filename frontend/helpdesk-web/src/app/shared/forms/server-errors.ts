import { AbstractControl, FormGroup } from '@angular/forms';

/**
 * Lleva los errores de validación de la API (ProblemDetails.errors) a los
 * controles del formulario. Las claves de la API están en camelCase y con
 * puntos ("additionalData.priority"), igual que las rutas de un FormGroup
 * anidado, así que form.get(clave) encuentra el control.
 *
 * @returns los mensajes que no corresponden a ningún control, para mostrarlos aparte.
 */
export function applyServerErrors(form: FormGroup, errors: Readonly<Record<string, string[]>>): string[] {
  const unmatched: string[] = [];

  for (const [field, messages] of Object.entries(errors)) {
    const control = form.get(field);

    if (control) {
      control.setErrors({ ...control.errors, server: messages.join(' ') });
      control.markAsTouched();
    } else {
      unmatched.push(...messages);
    }
  }

  return unmatched;
}

/** Mensaje del servidor guardado en el control por applyServerErrors. */
export function serverError(control: AbstractControl | null): string | null {
  const message: unknown = control?.errors?.['server'];

  return typeof message === 'string' ? message : null;
}
