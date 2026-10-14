import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SharedSvgRoutes } from '../../../../shared/constants/shared-svg-routes';
import { SvgIconComponent } from 'angular-svg-icon';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { VeStatusComponent } from '../../../../components/ve-status/ve-status.component';
import { TaskAssigneeComponent } from '../../../../components/task-assignee/task-assignee.component';
import { TaskPriorityComponent } from '../../../../components/task-priority/task-priority.component';
import { BoardWorkspaceStore } from '../../services/board-workspace.store';
import { TaskSessionService } from '../../services/task-session.service';

@Component({
  selector: 'app-backlog-view',
  templateUrl: './backlog-view.component.html',
  styleUrl: './backlog-view.component.scss',
  imports: [
    CommonModule,
    SvgIconComponent,
    NgbModule,
    VeStatusComponent,
    TaskAssigneeComponent,
    TaskPriorityComponent,
  ],
})
export class BacklogViewComponent {
  readonly store = inject(BoardWorkspaceStore);
  readonly taskSession = inject(TaskSessionService);
  readonly sharedSvgRoutes = SharedSvgRoutes;
}
