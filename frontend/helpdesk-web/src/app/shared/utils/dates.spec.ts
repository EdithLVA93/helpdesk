import { startOfToday, toIsoDate } from './dates';

describe('toIsoDate', () => {
  it('usa la fecha local, no UTC (las 23:30 no pasan al día siguiente)', () => {
    expect(toIsoDate(new Date(2026, 0, 5, 23, 30))).toBe('2026-01-05');
  });

  it('completa con ceros el mes y el día', () => {
    expect(toIsoDate(new Date(2027, 8, 3))).toBe('2027-09-03');
  });
});

describe('startOfToday', () => {
  it('devuelve hoy a medianoche', () => {
    const today = startOfToday();
    const now = new Date();

    expect([today.getHours(), today.getMinutes(), today.getSeconds()]).toEqual([0, 0, 0]);
    expect(today.getDate()).toBe(now.getDate());
  });
});
