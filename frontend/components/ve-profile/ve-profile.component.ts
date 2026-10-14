import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { SvgIconComponent } from 'angular-svg-icon';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import {
  ActiveWorkItemLockTracker,
  SignalRService,
  TaskService,
  UserService,
} from '../../shared/services';
import { AvatarComponent } from '../avatar/avatar.component';
import { Router } from '@angular/router';

@Component({
  selector: 'app-ve-profile',
  templateUrl: './ve-profile.component.html',
  styleUrl: './ve-profile.component.scss',
  imports: [CommonModule, SvgIconComponent, NgbModule, AvatarComponent],
})
export class VeProfileComponent implements OnInit {
  public onProfileShown = signal<boolean>(false);

  public user: any | null = null;

  public sharedSvgRoutes = SharedSvgRoutes;

  private readonly userService = inject(UserService);
  private readonly router = inject(Router);
  private readonly signalRService = inject(SignalRService);
  private readonly lockTracker = inject(ActiveWorkItemLockTracker);
  private readonly taskService = inject(TaskService);

  ngOnInit(): void {
    this.getUserProfile();
  }

  public getUserProfile() {
    this.userService.getUserById().subscribe({
      next: (result) => {
        this.user = result;
      },
    });
  }

  public onLogout() {
    this.lockTracker.release(this.taskService).subscribe({
      next: () => this.clearSessionAndLeave(),
      error: () => this.clearSessionAndLeave(),
    });
  }

  private clearSessionAndLeave(): void {
    localStorage.removeItem('user_id');
    localStorage.removeItem('token');
    void this.signalRService.disconnect();
    this.router.navigate(['/login']);
  }
}
