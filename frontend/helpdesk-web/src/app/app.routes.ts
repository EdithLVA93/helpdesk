import { Routes } from '@angular/router';

import { Shell } from './layout/shell/shell';

/**
 * Cada pantalla se carga de forma diferida (loadComponent): el navegador
 * solo descarga el código de la pantalla cuando el usuario entra en ella.
 */
export const routes: Routes = [
  {
    path: '',
    component: Shell,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'tickets' },
      {
        path: 'tickets',
        title: 'Tickets · HelpDesk',
        loadComponent: () =>
          import('./features/tickets/ticket-dashboard/ticket-dashboard').then((m) => m.TicketDashboard),
      },
      {
        path: 'tickets/new',
        title: 'Nuevo ticket · HelpDesk',
        loadComponent: () => import('./features/tickets/ticket-create/ticket-create').then((m) => m.TicketCreate),
      },
      {
        path: 'users',
        title: 'Usuarios · HelpDesk',
        loadComponent: () =>
          import('./features/users/user-management/user-management').then((m) => m.UserManagement),
      },
    ],
  },
  { path: '**', redirectTo: 'tickets' },
];
