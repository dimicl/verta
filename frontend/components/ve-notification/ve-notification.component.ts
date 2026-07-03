import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { SvgIconComponent } from 'angular-svg-icon';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { AppNotification } from '../../shared/interfaces/notification.interface';
import { SignalRService } from '../../shared/services';

@Component({
  selector: 'app-ve-notification',
  templateUrl: './ve-notification.component.html',
  styleUrl: './ve-notification.component.scss',
  imports: [SvgIconComponent, CommonModule, NgbModule],
})
export class VeNotificationComponent implements OnInit {
  public sharedSvgRoutes = SharedSvgRoutes;
  public onNotificationShown = signal<boolean>(false);
  public notifications = signal<AppNotification[]>([]);

  private readonly signalRService = inject(SignalRService);

  public unreadCount = computed(
    () => this.notifications().filter((item) => !item.read).length
  );

  public async ngOnInit(): Promise<void> {
    await this.signalRService.connect();

    this.signalRService.notifications$.subscribe((items) => {
      this.notifications.set(items);
    });
  }

  public onPopoverShown(): void {
    this.onNotificationShown.set(true);
    this.signalRService.markAllAsRead();
  }

  public onPopoverHidden(): void {
    this.onNotificationShown.set(false);
  }
}
