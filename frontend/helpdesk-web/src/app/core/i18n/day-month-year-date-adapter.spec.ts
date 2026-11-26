import { TestBed } from '@angular/core/testing';
import { MAT_DATE_LOCALE } from '@angular/material/core';

import { DayMonthYearDateAdapter } from './day-month-year-date-adapter';

describe('DayMonthYearDateAdapter', () => {
  let adapter: DayMonthYearDateAdapter;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [DayMonthYearDateAdapter, { provide: MAT_DATE_LOCALE, useValue: 'es-CO' }],
    });

    adapter = TestBed.inject(DayMonthYearDateAdapter);
  });

  function parts(date: Date | null): [number, number, number] | null {
    return date ? [date.getFullYear(), date.getMonth() + 1, date.getDate()] : null;
  }

  it('interpreta el formato colombiano dd/mm/aaaa', () => {
    expect(parts(adapter.parse('15/12/2026', null))).toEqual([2026, 12, 15]);
  });

  it('no confunde día y mes (01/02 es 1 de febrero, no 2 de enero)', () => {
    expect(parts(adapter.parse('01/02/2026', null))).toEqual([2026, 2, 1]);
  });

  it('acepta un solo dígito y separadores - o .', () => {
    expect(parts(adapter.parse('5-1-2027', null))).toEqual([2027, 1, 5]);
    expect(parts(adapter.parse('05.01.2027', null))).toEqual([2027, 1, 5]);
  });

  it('rechaza fechas imposibles en lugar de desbordarlas', () => {
    const result = adapter.parse('31/02/2026', null);

    expect(adapter.isValid(result as Date)).toBe(false);
  });

  it('rechaza texto que no es una fecha', () => {
    expect(adapter.isValid(adapter.parse('mañana', null) as Date)).toBe(false);
  });

  it('devuelve null si el campo está vacío', () => {
    expect(adapter.parse('', null)).toBeNull();
    expect(adapter.parse('   ', null)).toBeNull();
  });
});
