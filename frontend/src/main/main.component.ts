import { Component, OnInit, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import {
  BoardService,
  TaskService,
  UserService,
  WorkspaceService,
} from '../../shared/services';
import { CommonModule } from '@angular/common';
import { InputComponent } from '../../components/input/input.component';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { SvgIconComponent } from 'angular-svg-icon';
import { NgbModal, NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { AvatarComponent } from '../../components/avatar/avatar.component';
import { ChatComponent } from '../chat/chat.component';
import { VeNotificationComponent } from '../../components/ve-notification/ve-notification.component';
import { TaskModalComponent } from '../../components/task-modal/task-modal.component';
import { VeExtraMembersComponent } from '../../components/ve-extra-members/ve-extra-members.component';
import { WorkspaceModalComponent } from '../../components/workspace-modal/workspace-modal.component';
import { WorkspaceResponse } from '../../shared/interfaces/workspace-response.interface';
import {
  BoardResponse,
  WorkItemLockResponse,
  WorkItemRequest,
  WorkItemResponse,
} from '../../shared/interfaces';
import { InviteModalComponent } from '../../components/invite-modal/invite-modal.component';
import { VeProfileComponent } from '../../components/ve-profile/ve-profile.component';
import { VeStatusComponent } from '../../components/ve-status/ve-status.component';
import { BoardModalComponent } from '../../components/board-modal/board-modal.component';
import { TaskStatus } from '../../shared/types/task-status.type';

interface BoardTaskGroup {
  id: number;
  name: string;
  tasks: WorkItemResponse[];
}

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
    WorkspaceModalComponent,
    VeProfileComponent,
    VeStatusComponent,
  ],
})
export class MainComponent implements OnInit {
  public sharedSvgRoutes = SharedSvgRoutes;
  public readonly toolbarItems = ['Backlog', 'Board', 'Chat'] as const;
  public readonly selectedToolbarTab =
    signal<(typeof this.toolbarItems)[number]>('Backlog');

  public workspace: WorkspaceResponse | null = null;
  public boards: any | null = null;

  public workspaceMembers: Array<{ id: number; firstName: string; lastName: string }> = [];

  public boardTaskGroups: BoardTaskGroup[] = [];
  public statusOptions: TaskStatus[] = [
    'ToDo',
    'InProgress',
    'PR',
    'Testing',
    'Done',
  ];

  public backlogPhases = ['ToDo', 'In Progress', 'PR', 'Testing', 'Done'] as const;

  public taskMenuOpenId: number | null = null;
  public activeWorkItemId: number | null = null;
  public stickyLockMessage = '';
  public isGroupsExpanded = true;
  public showLockToast = false;

  selectedUser = signal<number | null>(null);

  public ngOnInit(): void {
    this.userService.getUserById().subscribe((response) => {
      console.log(response);
    });

    this.getWorkspace();
  }

  constructor(
    private userService: UserService,
    private workspaceService: WorkspaceService,
    private boardService: BoardService,
    private taskService: TaskService,
    private modalService: NgbModal
  ) {}

  private getWorkspace() {
    this.workspaceService.getWorkspace().subscribe({
      next: (result) => {
        this.workspace = result;
        this.getBoards();
        if (this.workspace?.id) {
          this.getWorkspaceMembers(this.workspace.id);
        }
      },
    });
  }

  private getWorkspaceMembers(workspaceId: number): void {
    this.workspaceService.getMembers(workspaceId).subscribe({
      next: (members) => {
        this.workspaceMembers = members.map((member: any) => ({
          id: member.userId,
          firstName: member.firstName || 'User',
          lastName: member.lastName || '',
        }));
      },
      error: (err) => {
        console.error('Greška pri učitavanju članova radnog prostora:', err);
      },
    });
  }

  private loadWorkItems(boards: BoardResponse[]): void {
    if (!boards || boards.length === 0) {
      this.boardTaskGroups = [];
      return;
    }

    forkJoin(boards.map((board) => this.taskService.getByBoardId(board.id))).subscribe({
      next: (taskLists) => {
        this.boardTaskGroups = boards.map((board, index) => ({
          id: board.id,
          name: board.name,
          tasks: taskLists[index] ?? [],
        }));
      },
      error: (err) => {
        console.error('Greška pri učitavanju taskova:', err);
        this.boardTaskGroups = [];
      },
    });
  }

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

  public onUserSelect(userId: number): void {
    this.selectedUser.set(userId);
  }

  public onAddTask(): void {
    const modalRef = this.modalService.open(TaskModalComponent, {
      backdrop: 'static',
      keyboard: false,
    });

    modalRef.result.then(
      (result) => {
        if (result?.title && this.boards?.length) {
          this.createTask(result);
        }
      },
      () => {}
    );
  }

  private createTask(result: {
    title: string;
    description: string;
    comment?: string;
    status: TaskStatus;
    points: number | null;
  }): void {
    const board = (this.boards as BoardResponse[])[0];
    if (!board) {
      return;
    }

    const request: WorkItemRequest = {
      name: result.title,
      description: result.description,
      boardId: board.id,
      priority: 'Medium',
    };

    this.taskService.create(request).subscribe({
      next: (createdTask) => {
        const comment = result.comment?.trim();
        const completeSave = () => {
          if (result.status !== 'ToDo') {
            this.taskService.changeStatus(createdTask.id, result.status).subscribe({
              next: () => this.loadWorkItems(this.boards as BoardResponse[]),
              error: (err) => {
                console.error('Greška pri promeni statusa taska:', err);
                this.loadWorkItems(this.boards as BoardResponse[]);
              },
            });
          } else {
            this.loadWorkItems(this.boards as BoardResponse[]);
          }
        };

        if (comment) {
          this.taskService.createComment({ workItemId: createdTask.id, content: comment }).subscribe({
            next: () => completeSave(),
            error: (err) => {
              console.error('Greška pri kreiranju komentara:', err);
              completeSave();
            },
          });
        } else {
          completeSave();
        }
      },
      error: (err) => {
        console.error('Greška pri kreiranju taska:', err);
      },
    });
  }

  public onTaskStatusChange(task: WorkItemResponse, status: TaskStatus): void {
    if (task.status === status) {
      return;
    }

    this.taskService.changeStatus(task.id, status).subscribe({
      next: (updated) => {
        task.status = updated.status;
      },
      error: (err) => {
        console.error('Greška pri promeni statusa taska:', err);
      },
    });
  }

  public onToggleTaskOptions(taskId: number): void {
    this.taskMenuOpenId = this.taskMenuOpenId === taskId ? null : taskId;
  }

  public onOpenTask(task: WorkItemResponse): void {
    this.taskService.openWorkItem(task.id).subscribe({
      next: (lock) => {
        if (lock.mode === 'WRITE') {
          this.activeWorkItemId = task.id;
          this.stickyLockMessage = '';
          this.openTaskModal(task, true);
        } else {
          this.stickyLockMessage =
            'Task je trenutno zaključan.';
          this.showLockToast = true;
          setTimeout(() => {
            this.showLockToast = false;
          }, 2500);
        }
      },
      error: (err) => {
        console.error('Greška pri otvaranju taska:', err);
        this.stickyLockMessage =
          'Došlo je do greške pri otvaranju taska. Pokušajte ponovo.';
      },
    });
  }

  private openTaskModal(task: WorkItemResponse, isEditMode: boolean): void {
    const modalRef = this.modalService.open(TaskModalComponent, {
      backdrop: 'static',
      keyboard: false,
      size: 'lg',
    });

    modalRef.componentInstance.isEditMode = isEditMode;
    modalRef.componentInstance.title = task.name;
    modalRef.componentInstance.description = task.description;
    modalRef.componentInstance.comment = '';
    modalRef.componentInstance.status = task.status;
    modalRef.componentInstance.points = null;

    modalRef.result.then(
      (result) => {
        if (result && isEditMode) {
          this.updateTask(task.id, result, task.boardId);
        }
        this.closeTask(task.id);
      },
      () => {
        this.closeTask(task.id);
      }
    );
  }

  private closeTask(taskId: number): void {
    this.taskService.closeWorkItem(taskId).subscribe({
      next: () => {
        if (this.activeWorkItemId === taskId) {
          this.activeWorkItemId = null;
        }
      },
      error: (err) => {
        console.error('Greška pri zatvaranju task zaključavanja:', err);
      },
    });
  }

  public onEditTask(task: WorkItemResponse): void {
    this.onOpenTask(task);
  }

  public onDeleteTask(task: WorkItemResponse): void {
    if (!confirm('Želite li sigurno da obrišete ovaj task?')) {
      return;
    }

    this.taskService.delete(task.id).subscribe({
      next: () => {
        this.loadWorkItems(this.boards as BoardResponse[]);
      },
      error: (err) => {
        console.error('Greška pri brisanju taska:', err);
      },
    });
  }

  public onReleaseTask(task: WorkItemResponse): void {
    if (this.activeWorkItemId !== task.id) {
      return;
    }

    this.closeTask(task.id);
  }

  private updateTask(workItemId: number, result: {
    title: string;
    description: string;
    comment?: string;
    status: TaskStatus;
    points: number | null;
  }, boardId: number): void {
    const request: WorkItemRequest = {
      name: result.title,
      description: result.description,
      boardId,
      priority: 'Medium',
    };

    this.taskService.update(workItemId, request).subscribe({
      next: (updatedTask) => {
        const comment = result.comment?.trim();
        const completeSave = () => {
          if (result.status !== 'ToDo') {
            this.taskService.changeStatus(updatedTask.id, result.status).subscribe({
              next: () => this.loadWorkItems(this.boards as BoardResponse[]),
              error: (err) => {
                console.error('Greška pri promeni statusa taska:', err);
                this.loadWorkItems(this.boards as BoardResponse[]);
              },
            });
          } else {
            this.loadWorkItems(this.boards as BoardResponse[]);
          }
        };

        if (comment) {
          this.taskService.createComment({ workItemId: updatedTask.id, content: comment }).subscribe({
            next: () => completeSave(),
            error: (err) => {
              console.error('Greška pri kreiranju komentara:', err);
              completeSave();
            },
          });
        } else {
          completeSave();
        }
      },
      error: (err) => {
        console.error('Greška pri ažuriranju taska:', err);
      },
    });
  }

  public getBoards(): void {
    if (!this.workspace?.id) {
      return;
    }

    this.boardService.getBoards(this.workspace.id).subscribe({
      next: (result) => {
        this.boards = result;
        this.loadWorkItems(result);
      },
      error: (err) => {
        console.error('Greška pri učitavanju tabli:', err);
      },
    });
  }

  public onHeaderGroupsToggle(): void {
    this.isGroupsExpanded = !this.isGroupsExpanded;
  }

  public hasWorkspaceId = signal<boolean>(false);

  public onGetWorkspace() {
    this.hasWorkspaceId.set(true);
  }

  public onOpenCreateModal() {
    this.modalService.open(WorkspaceModalComponent, {});
  }

  public onOpenInviteModal() {
    const modalRef = this.modalService.open(InviteModalComponent, {
      backdrop: 'static',
      keyboard: false,
    });
    modalRef.componentInstance.workspaceId = this.workspace?.id;

    modalRef.result.then(
      () => {
        if (this.workspace?.id) {
          this.getWorkspaceMembers(this.workspace.id);
          this.getBoards();
        }
      },
      () => {
        // modal dismissed without invite
      }
    );
  }

  public onAddBoard() {
    this.modalService.open(BoardModalComponent, {});
  }
}
