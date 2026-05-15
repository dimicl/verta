import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { SvgIconComponent } from 'angular-svg-icon';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { UserService } from '../../shared/services';
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

  ngOnInit(): void {
    this.getUserProfile();
  }

  constructor(private userService: UserService, private router: Router) {}

  public getUserProfile() {
    this.userService.getUserById().subscribe({
      next: (result) => {
        console.log('res', result);
        this.user = result;
      },
    });
  }

  public onLogout() {
    localStorage.removeItem('user_id');
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }
}
