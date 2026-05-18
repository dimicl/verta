import { Component } from '@angular/core';
import { SvgIconComponent } from 'angular-svg-icon';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { TaskStatus } from '../../shared/types';

@Component({
  selector: 'app-ve-status',
  imports: [SvgIconComponent],
  templateUrl: './ve-status.component.html',
  styleUrl: './ve-status.component.scss',
})
export class VeStatusComponent {
  public sharedSvgRoutes = SharedSvgRoutes;

  public taskStatus: TaskStatus = 'ToDo';
}
