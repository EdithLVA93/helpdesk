import { Injectable } from '@angular/core';
import { NativeDateAdapter } from '@angular/material/core';

const DAY_MONTH_YEAR = /^\s*(\d{1,2})[/.-](\d{1,2})[/.-](\d{4})\s*$/;

/**
 * NativeDateAdapter interpreta lo que se escribe con Date.parse, que no
 * entiende el formato colombiano: "15/12/2026" se rechaza como inválido.
 * Este adaptador interpreta día/mes/año y descarta fechas imposibles
 * (31/02/2026). Para mostrar fechas se sigue usando el formato de es-CO.
 */
@Injectable()
export class DayMonthYearDateAdapter extends NativeDateAdapter {
  override parse(value: unknown, parseFormat?: unknown): Date | null {
    if (typeof value !== 'string') {
      return super.parse(value, parseFormat);
    }

    if (value.trim() === '') {
      return null;
    }

    const match = DAY_MONTH_YEAR.exec(value);

    if (!match) {
      return this.invalid();
    }

    const day = Number(match[1]);
    const month = Number(match[2]);
    const year = Number(match[3]);
    const date = new Date(year, month - 1, day);

    // new Date(2026, 1, 31) "se desborda" al 3 de marzo: se comprueba que no haya cambiado.
    const isRealDate = date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;

    return isRealDate ? date : this.invalid();
  }
}
