import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Como Validators.required, pero también rechaza textos con solo espacios
 * ("   "), igual que el backend (NotEmpty de FluentValidation).
 */
export const notBlank: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value: unknown = control.value;

  return typeof value === 'string' && value.trim().length === 0 ? { required: true } : null;
};

/** Mismo criterio que el backend: texto@dominio.extensión, sin espacios. */
export const emailFormat: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value: unknown = control.value;

  if (typeof value !== 'string' || value.trim().length === 0) {
    return null;
  }

  return /^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(value.trim()) ? null : { email: true };
};
