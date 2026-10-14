import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SharedSvgRoutes } from '../../../../shared/constants/shared-svg-routes';
import { SvgIconComponent } from 'angular-svg-icon';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { VeStatusComponent } from '../../../../components/ve-status/ve-status.component';
import { TaskAssigneeComponent } from '../../../../components/task-assignee/task-assignee.component';
import { TaskPriorityComponent } from '../../../../components/task-priority/task-priority.component';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { BoardWorkspaceStore } from '../../services/board-workspace.store';
import { TaskSessionService } from '../../services/task-session.service';

@Component({
  selector: 'app-kanban-board',
  templateUrl: './kanban-board.component.html',
  styleUrl: './kanban-board.component.scss',
  imports: [
    CommonModule,
    SvgIconComponent,
    NgbModule,
    VeStatusComponent,
    TaskAssigneeComponent,
    TaskPriorityComponent,
    DragDropModule,
  ],
})
export class KanbanBoardComponent {
  readonly store = inject(BoardWorkspaceStore);
  readonly taskSession = inject(TaskSessionService);
  readonly sharedSvgRoutes = SharedSvgRoutes;
}
