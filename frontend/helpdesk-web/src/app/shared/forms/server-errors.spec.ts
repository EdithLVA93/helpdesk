import { FormControl, FormGroup } from '@angular/forms';

import { applyServerErrors, serverError } from './server-errors';

describe('applyServerErrors', () => {
  function buildForm() {
    return new FormGroup({
      title: new FormControl(''),
      additionalData: new FormGroup({
        priority: new FormControl(null),
      }),
    });
  }

  it('coloca cada error de la API en su control, también en grupos anidados', () => {
    const form = buildForm();

    applyServerErrors(form, {
      title: ['El título del ticket es obligatorio.'],
      'additionalData.priority': ['La prioridad es obligatoria.'],
    });

    expect(serverError(form.get('title'))).toBe('El título del ticket es obligatorio.');
    expect(serverError(form.get('additionalData.priority'))).toBe('La prioridad es obligatoria.');
    expect(form.get('title')?.touched).toBe(true);
  });

  it('devuelve los mensajes que no corresponden a ningún control', () => {
    const form = buildForm();

    const unmatched = applyServerErrors(form, {
      'additionalData.tags[0]': ['Cada etiqueta puede tener como máximo 30 caracteres.'],
    });

    expect(unmatched).toEqual(['Cada etiqueta puede tener como máximo 30 caracteres.']);
  });

  it('une varios mensajes del mismo campo', () => {
    const form = buildForm();

    applyServerErrors(form, { title: ['Primero.', 'Segundo.'] });

    expect(serverError(form.get('title'))).toBe('Primero. Segundo.');
  });
});
