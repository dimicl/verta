import { Component, OnInit, signal } from '@angular/core';
import { UserService } from '../../shared/services';
import { CommonModule } from '@angular/common';
import { InputComponent } from '../../components/input/input.component';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { SvgIconComponent } from 'angular-svg-icon';
import { NgbModal, NgbModule, NgbPopover } from '@ng-bootstrap/ng-bootstrap';
import { AvatarComponent } from '../../components/avatar/avatar.component';
import { ChatComponent } from '../chat/chat.component';
import { VeNotificationComponent } from '../../components/ve-notification/ve-notification.component';
import { TaskModalComponent } from '../../components/task-modal/task-modal.component';
import { VeExtraMembersComponent } from '../../components/ve-extra-members/ve-extra-members.component';

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
    ChatComponent,
    VeNotificationComponent,
    VeExtraMembersComponent,
  ],
})
export class MainComponent implements OnInit {
  public sharedSvgRoutes = SharedSvgRoutes;
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

  sprints = [
    {
      id: 1,
      name: 'Sprint 1',
      from: '',
      to: '',
      description: 'Ovo je neki opis za koliko se zavrsava',
    },
    {
      id: 2,
      name: 'Backlog',
      from: '',
      to: '',
      description: 'Ovo je neki opis za koliko se zavrsava',
    },
  ];

  public isGroupsExpanded = true;

  selectedUser = signal<number | null>(null);

  constructor(
    private userService: UserService,
    private modalService: NgbModal
  ) {}

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

  public onAddTask(): void {
    this.modalService.open(TaskModalComponent, {
      backdrop: 'static',
      keyboard: false,
    });
  }

  public onHeaderGroupsToggle(): void {
    this.isGroupsExpanded = !this.isGroupsExpanded;
  }
}
