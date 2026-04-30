import { Component, OnInit, signal } from '@angular/core';
import { UserService } from '../../shared/services';
import { CommonModule } from '@angular/common';
import { InputComponent } from '../../components/input/input.component';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { SvgIconComponent } from 'angular-svg-icon';
import { NgbModule, NgbPopover } from '@ng-bootstrap/ng-bootstrap';
import { AvatarComponent } from '../../components/avatar/avatar.component';
import { ChatComponent } from '../chat/chat.component';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss',
  imports: [
    CommonModule,
    InputComponent,
    SvgIconComponent,
    AvatarComponent,
    NgbModule,
    NgbPopover,
    ChatComponent,
  ],
})
export class MainComponent implements OnInit {
  public sharedSvgRoutes = SharedSvgRoutes;
  onNotificationShown = signal<boolean>(false);
  public readonly toolbarItems = ['Backlog', 'Board', 'Chat'] as const;
  public readonly selectedToolbarTab =
    signal<(typeof this.toolbarItems)[number]>('Backlog');

  users = [
    {
      id: 1,
      firstName: 'John',
      lastName: 'Doe',
    },
    {
      id: 2,
      firstName: 'Jane',
      lastName: 'Becks',
    },
    {
      id: 3,
      firstName: 'Laura',
      lastName: 'Smith',
    },
  ];

  selectedUser = signal<number | null>(null);

  constructor(private userService: UserService) {}

  public onToolbarTabSelect(tab: (typeof this.toolbarItems)[number]): void {
    this.selectedToolbarTab.set(tab);
  }

  public getToolbarSliderLeft(): string {
    const selectedIndex = this.toolbarItems.indexOf(this.selectedToolbarTab());
    const safeIndex = selectedIndex < 0 ? 0 : selectedIndex;
    const tabWidth = 100 / this.toolbarItems.length;

    return `${safeIndex * tabWidth}%`;
  }

  public getToolbarSliderWidth(): string {
    return `${100 / this.toolbarItems.length}%`;
  }

  public ngOnInit(): void {
    this.userService
      .getUserById(localStorage.getItem('user_id') ?? '')
      .subscribe((response) => {
        console.log(response);
      });
  }

  public onUserSelect(userId: number): void {
    this.selectedUser.set(userId);
  }
}
