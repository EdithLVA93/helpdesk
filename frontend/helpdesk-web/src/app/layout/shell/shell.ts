import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, inject, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { map } from 'rxjs';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  exact: boolean;
}

/**
 * Estructura común de la aplicación: menú lateral y barra superior.
 * En pantallas pequeñas el menú se oculta y se abre con el botón de la barra.
 */
@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Shell {
  private readonly sidenav = viewChild.required(MatSidenav);

  protected readonly isHandset = toSignal(
    inject(BreakpointObserver)
      .observe('(max-width: 959.98px)')
      .pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  protected readonly navItems: readonly NavItem[] = [
    { label: 'Tickets', icon: 'confirmation_number', route: '/tickets', exact: true },
    { label: 'Nuevo ticket', icon: 'add_circle', route: '/tickets/new', exact: true },
    { label: 'Usuarios', icon: 'group', route: '/users', exact: true },
  ];

  protected toggleMenu(): void {
    void this.sidenav().toggle();
  }

  /** En móvil, cierra el menú al navegar. */
  protected closeOnHandset(): void {
    if (this.isHandset()) {
      void this.sidenav().close();
    }
  }
}
