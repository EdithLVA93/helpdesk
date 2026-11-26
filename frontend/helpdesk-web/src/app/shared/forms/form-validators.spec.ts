import { FormControl } from '@angular/forms';

import { emailFormat, notBlank } from './form-validators';

describe('notBlank', () => {
  it.each(['', '   ', '\t'])('rechaza "%s" como obligatorio', (value) => {
    expect(notBlank(new FormControl(value))).toEqual({ required: true });
  });

  it('acepta texto con contenido', () => {
    expect(notBlank(new FormControl('  VPN caída  '))).toBeNull();
  });

  it('no opina sobre valores que no son texto (lo hace Validators.required)', () => {
    expect(notBlank(new FormControl(null))).toBeNull();
  });
});

describe('emailFormat', () => {
  it.each(['ana@example.com', 'ana.torres+ti@empresa.com.co', '  ana@example.com  '])('acepta %s', (email) => {
    expect(emailFormat(new FormControl(email))).toBeNull();
  });

  it.each(['ana@', '@example.com', 'ana@example', 'ana torres@example.com', 'ana@@example.com'])(
    'rechaza %s',
    (email) => {
      expect(emailFormat(new FormControl(email))).toEqual({ email: true });
    },
  );

  it('deja el campo vacío a Validators.required', () => {
    expect(emailFormat(new FormControl(''))).toBeNull();
  });
});
