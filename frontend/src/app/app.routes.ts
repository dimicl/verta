import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'main',
    loadChildren: () =>
      import('../main/main.component').then((m) => m.MainComponent),
    /*     resolve: { main: MainResolver },
     */
  },
];
