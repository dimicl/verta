import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { NgbModule, NgbPopover } from '@ng-bootstrap/ng-bootstrap';
import { SvgIconComponent } from 'angular-svg-icon';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';

@Component({
  selector: 'app-ve-notification',
  templateUrl: './ve-notification.component.html',
  styleUrl: './ve-notification.component.scss',
  imports: [SvgIconComponent, CommonModule, NgbModule, NgbPopover],
})
export class VeNotificationComponent {
  public onNotificationShown = signal<boolean>(false);

  public sharedSvgRoutes = SharedSvgRoutes;
}
