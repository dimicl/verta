import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import {
  BoardService,
  CommentService,
  SubWorkItemService,
  WorkItemFileService,
  SignalRService,
  SprintService,
  TaskService,
  UserService,
  WorkspaceService,
} from '../../shared/services';
import { CommonModule } from '@angular/common';
import { InputComponent } from '../../components/input/input.component';
import { SharedSvgRoutes } from '../../shared/constants/shared-svg-routes';
import { SvgIconComponent } from 'angular-svg-icon';
import { NgbModal, NgbModule, NgbPopover } from '@ng-bootstrap/ng-bootstrap';
import { AvatarComponent } from '../../components/avatar/avatar.component';
import { ChatComponent } from '../chat/chat.component';
import { VeNotificationComponent } from '../../components/ve-notification/ve-notification.component';
import { TaskModalComponent } from '../../components/task-modal/task-modal.component';
import { STACKED_MODAL_OPTIONS, TASK_MODAL_OPTIONS } from '../../shared/constants/modal-options';
import { SprintModalComponent } from '../../components/sprint-modal/sprint-modal.component';
import { VeExtraMembersComponent } from '../../components/ve-extra-members/ve-extra-members.component';
import { WorkspaceModalComponent } from '../../components/workspace-modal/workspace-modal.component';
import { WorkspaceResponse } from '../../shared/interfaces/workspace-response.interface';
import {
  BoardResponse,
  SprintResponse,
  SubWorkItemResponse,
  WorkItemRequest,
  WorkItemResponse,
} from '../../shared/interfaces';
import { InviteModalComponent } from '../../components/invite-modal/invite-modal.component';
import { VeProfileComponent } from '../../components/ve-profile/ve-profile.component';
import { VeStatusComponent } from '../../components/ve-status/ve-status.component';
import { TaskAssigneeComponent } from '../../components/task-assignee/task-assignee.component';
import { TaskPriorityComponent } from '../../components/task-priority/task-priority.component';
import { BoardModalComponent } from '../../components/board-modal/board-modal.component';
import { TaskStatus } from '../../shared/types/task-status.type';
import { TaskPriority } from '../../shared/types/task-priority.type';
import {
  BOARD_COLUMNS,
  getAllowedNextStatuses,
} from '../../shared/helpers/task-status.helper';
import {
  CdkDragDrop,
  DragDropModule,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';

interface SprintTaskGroup {
  id: number | null;
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
    TaskAssigneeComponent,
    TaskPriorityComponent,
    DragDropModule,
  ],
})
export class MainComponent implements OnInit, OnDestroy {
  public sharedSvgRoutes = SharedSvgRoutes;
  public readonly toolbarItems = ['Backlog', 'Board', 'Chat'] as const;
  public readonly selectedToolbarTab =
    signal<(typeof this.toolbarItems)[number]>('Backlog');

  public workspace: WorkspaceResponse | null = null;
  public boards: BoardResponse[] | null = null;
  public selectedBoardId = signal<number | null>(null);

  public workspaceMembers: Array<{
    id: number;
    firstName: string;
    lastName: string;
  }> = [];

  public currentUser: {
    id: number;
    firstName: string;
    lastName: string;
  } | null = null;

  public boardMembers: Array<{
    id: number;
    firstName: string;
    lastName: string;
  }> = [];

  private readonly maxVisibleBoardMembers = 5;

  public sprintTaskGroups: SprintTaskGroup[] = [];
  public sprints: SprintResponse[] = [];
  private allTasks: WorkItemResponse[] = [];
  public searchQuery = signal('');
  public showStatusErrorToast = false;
  public statusErrorMessage = '';

  public readonly boardColumns = BOARD_COLUMNS;

  public boardTasksByStatus: Record<TaskStatus, WorkItemResponse[]> = {
    ToDo: [],
    InProgress: [],
    PR: [],
    Testing: [],
    Done: [],
  };

  public taskMenuOpenId: number | null = null;
  public activeWorkItemId: number | null = null;
  public stickyLockMessage = '';
  public collapsedGroupKeys = signal<Set<string>>(new Set());
  public expandedTaskIds = signal<Set<number>>(new Set());
  public showLockToast = false;

  public subtasksByWorkItemId: Record<number, SubWorkItemResponse[]> = {};

  private heartbeatTimer: ReturnType<typeof setInterval> | null = null;
  private lockToastTimer: ReturnType<typeof setTimeout> | null = null;

  selectedUser = signal<number | null>(null);

  public ngOnInit(): void {
    this.userService.getUserById().subscribe({
      next: (response: any) => {
        this.currentUser = {
          id: Number(response?.id ?? 0),
          firstName: response?.firstName ?? 'User',
          lastName: response?.lastName ?? '',
        };
      },
      error: (err) => console.error('Greška pri učitavanju korisnika:', err),
    });

    void this.signalRService.connect();

    this.getWorkspace();
  }

  public ngOnDestroy(): void {
    this.stopHeartbeat();
    if (this.lockToastTimer) {
      clearTimeout(this.lockToastTimer);
    }
    if (this.activeWorkItemId !== null) {
      this.taskService.closeWorkItem(this.activeWorkItemId).subscribe();
    }
  }

  constructor(
    private userService: UserService,
    private workspaceService: WorkspaceService,
    private boardService: BoardService,
    private sprintService: SprintService,
    private taskService: TaskService,
    private commentService: CommentService,
    private subWorkItemService: SubWorkItemService,
    private workItemFileService: WorkItemFileService,
    private signalRService: SignalRService,
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

  private mapMembers(
    members: Array<{ userId: number; firstName?: string; lastName?: string }>
  ) {
    return members.map((member) => ({
      id: member.userId,
      firstName: member.firstName || 'User',
      lastName: member.lastName || '',
    }));
  }

  private getWorkspaceMembers(workspaceId: number): void {
    this.workspaceService.getMembers(workspaceId).subscribe({
      next: (members) => {
        this.workspaceMembers = this.mapMembers(members);
      },
      error: (err) => {
        console.error('Greška pri učitavanju članova radnog prostora:', err);
      },
    });
  }

  private getBoardMembers(boardId: number): void {
    this.boardService.getBoardMembers(boardId).subscribe({
      next: (members) => {
        this.boardMembers = this.mapMembers(members);
      },
      error: (err) => {
        console.error('Greška pri učitavanju članova table:', err);
        this.boardMembers = [];
      },
    });
  }

  private loadBacklog(boardId: number): void {
    this.getBoardMembers(boardId);

    forkJoin({
      sprints: this.sprintService.getByBoardId(boardId),
      tasks: this.taskService.getByBoardId(boardId),
    }).subscribe({
      next: ({ sprints, tasks }) => {
        this.sprints = sprints;
        this.allTasks = tasks;
        this.applyTaskFilters();
        this.loadSubtasksForTasks(tasks);
      },
      error: (err) => {
        console.error('Greška pri učitavanju backloga:', err);
        this.sprintTaskGroups = [];
        this.sprints = [];
        this.allTasks = [];
        this.subtasksByWorkItemId = {};
        this.clearBoardTasks();
      },
    });
  }

  private clearBoardTasks(): void {
    this.boardTasksByStatus = {
      ToDo: [],
      InProgress: [],
      PR: [],
      Testing: [],
      Done: [],
    };
  }

  private applyTaskFilters(): void {
    const filteredTasks = this.getFilteredTasks();

    const groups: SprintTaskGroup[] = this.sprints.map((sprint) => ({
      id: sprint.id,
      name: sprint.name,
      tasks: filteredTasks.filter((task) => task.sprintId === sprint.id),
    }));

    groups.push({
      id: null,
      name: 'Backlog',
      tasks: filteredTasks.filter((task) => !task.sprintId),
    });

    this.sprintTaskGroups = groups;
    this.rebuildBoardTasks(filteredTasks);
  }

  private loadSubtasksForTasks(tasks: WorkItemResponse[]): void {
    if (tasks.length === 0) {
      this.subtasksByWorkItemId = {};
      return;
    }

    forkJoin(
      tasks.map((task) =>
        this.subWorkItemService.getByWorkItemId(task.id).pipe(
          catchError(() => of([] as SubWorkItemResponse[]))
        )
      )
    ).subscribe({
      next: (results) => {
        const map: Record<number, SubWorkItemResponse[]> = {};
        tasks.forEach((task, index) => {
          map[task.id] = results[index];
        });
        this.subtasksByWorkItemId = map;
      },
      error: (err) => {
        console.error('Greška pri učitavanju podzadataka:', err);
        this.subtasksByWorkItemId = {};
      },
    });
  }

  public hasSubtasks(taskId: number): boolean {
    return (this.subtasksByWorkItemId[taskId]?.length ?? 0) > 0;
  }

  public getSubtasks(taskId: number): SubWorkItemResponse[] {
    return this.subtasksByWorkItemId[taskId] ?? [];
  }

  public isTaskExpanded(taskId: number): boolean {
    return this.expandedTaskIds().has(taskId);
  }

  public onToggleTaskSubtasks(taskId: number, event: Event): void {
    event.stopPropagation();

    this.expandedTaskIds.update((ids) => {
      const next = new Set(ids);
      if (next.has(taskId)) {
        next.delete(taskId);
      } else {
        next.add(taskId);
      }
      return next;
    });
  }

  public onSubtaskStatusChange(
    taskId: number,
    subtask: SubWorkItemResponse,
    status: TaskStatus
  ): void {
    if (subtask.status === status) {
      return;
    }

    this.subWorkItemService.changeStatus(subtask.id, status).subscribe({
      next: (updated) => {
        const list = this.subtasksByWorkItemId[taskId];
        if (!list) {
          return;
        }

        const index = list.findIndex((item) => item.id === updated.id);
        if (index >= 0) {
          list[index] = updated;
        }
      },
      error: (err) => {
        console.error('Greška pri promeni statusa podzadatka:', err);
        this.showStatusError(err.error?.message ?? err.message);
      },
    });
  }

  public onOpenSubtask(task: WorkItemResponse, subtask: SubWorkItemResponse): void {
    const modalRef = this.modalService.open(TaskModalComponent, STACKED_MODAL_OPTIONS);

    modalRef.componentInstance.isSubtask = true;
    modalRef.componentInstance.isEditMode = true;
    modalRef.componentInstance.parentWorkItemId = task.id;
    modalRef.componentInstance.parentTaskTitle = task.name;
    modalRef.componentInstance.subWorkItemId = subtask.id;
    modalRef.componentInstance.title = subtask.name;
    modalRef.componentInstance.description = subtask.description;
    modalRef.componentInstance.status = subtask.status;
    modalRef.componentInstance.priority = subtask.priority ?? 'Medium';
    modalRef.componentInstance.assignedUserId = subtask.assignedUserId ?? null;
    modalRef.componentInstance.members = this.boardMembers;
    this.setModalCurrentUser(modalRef.componentInstance);

    modalRef.result.then(
      (result) => {
        if (result?.title) {
          this.saveSubtaskFromBoard(task, subtask, result);
        }
      },
      () => {}
    );
  }

  private saveSubtaskFromBoard(
    task: WorkItemResponse,
    existing: SubWorkItemResponse,
    result: {
      title: string;
      description: string;
      status: TaskStatus;
      priority: TaskPriority;
      assignedUserId?: number | null;
    }
  ): void {
    this.subWorkItemService
      .update(existing.id, {
        name: result.title.trim(),
        description: result.description.trim(),
        assignedUserId: result.assignedUserId,
        priority: result.priority,
      })
      .subscribe({
        next: (updated) => {
          const applyStatus = (item: SubWorkItemResponse) => {
            const list = this.subtasksByWorkItemId[task.id];
            if (list) {
              const index = list.findIndex((entry) => entry.id === item.id);
              if (index >= 0) {
                list[index] = item;
              }
            }
          };

          if (result.status !== existing.status) {
            this.subWorkItemService.changeStatus(updated.id, result.status).subscribe({
              next: (withStatus) => applyStatus(withStatus),
              error: () => applyStatus(updated),
            });
            return;
          }

          applyStatus(updated);
        },
        error: (err) => {
          console.error('Greška pri ažuriranju podzadatka:', err);
          this.showStatusError(err.error?.message ?? err.message);
        },
      });
  }

  private getFilteredTasks(): WorkItemResponse[] {
    const query = this.searchQuery().trim().toLowerCase();
    const userId = this.selectedUser();

    return this.allTasks.filter((task) => {
      const matchesSearch =
        !query || task.name.toLowerCase().includes(query);
      const matchesAssignee =
        userId === null || task.assignedUserId === userId;

      return matchesSearch && matchesAssignee;
    });
  }

  public hasActiveTaskFilters(): boolean {
    return (
      this.searchQuery().trim().length > 0 || this.selectedUser() !== null
    );
  }

  public onSearchChange(query: string): void {
    this.searchQuery.set(query);
    this.applyTaskFilters();
  }

  private rebuildBoardTasks(tasks: WorkItemResponse[]): void {
    const grouped: Record<TaskStatus, WorkItemResponse[]> = {
      ToDo: [],
      InProgress: [],
      PR: [],
      Testing: [],
      Done: [],
    };

    for (const task of tasks) {
      grouped[task.status].push(task);
    }

    this.boardTasksByStatus = grouped;
  }

  public get boardColumnIds(): TaskStatus[] {
    return this.boardColumns.map((column) => column.status);
  }

  public getAssigneeName(task: WorkItemResponse): string {
    if (!task.assignedUserId) {
      return 'Unassigned';
    }

    const member = this.boardMembers.find((m) => m.id === task.assignedUserId);
    if (!member) {
      return 'Unassigned';
    }

    return `${member.firstName} ${member.lastName}`.trim();
  }

  public onBoardDrop(
    event: CdkDragDrop<WorkItemResponse[]>,
    targetStatus: TaskStatus
  ): void {
    if (event.previousContainer === event.container) {
      moveItemInArray(
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
      return;
    }

    const task = event.previousContainer.data[event.previousIndex];
    const previousStatus = task.status;

    if (!getAllowedNextStatuses(previousStatus).includes(targetStatus)) {
      this.showStatusError('Invalid status transition.');
      return;
    }

    transferArrayItem(
      event.previousContainer.data,
      event.container.data,
      event.previousIndex,
      event.currentIndex
    );

    task.status = targetStatus;

    const taskInAll = this.allTasks.find((item) => item.id === task.id);
    if (taskInAll) {
      taskInAll.status = targetStatus;
    }

    this.taskService.changeStatus(task.id, targetStatus).subscribe({
      next: (updated) => {
        task.status = updated.status;
      },
      error: (err) => {
        transferArrayItem(
          event.container.data,
          event.previousContainer.data,
          event.currentIndex,
          event.previousIndex
        );
        task.status = previousStatus;

        console.error('Greška pri promeni statusa taska:', err);
        if (err.status === 403) {
          this.showTaskOccupiedToast();
          return;
        }
        this.showStatusError(err.error?.message ?? err.message);
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
    this.selectedUser.update((current) => (current === userId ? null : userId));
    this.applyTaskFilters();
  }

  public get visibleBoardMembers(): Array<{
    id: number;
    firstName: string;
    lastName: string;
  }> {
    return this.boardMembers.slice(0, this.maxVisibleBoardMembers);
  }

  public get hiddenBoardMembers(): Array<{
    id: number;
    firstName: string;
    lastName: string;
  }> {
    return this.boardMembers.slice(this.maxVisibleBoardMembers);
  }

  public get extraBoardMembersCount(): number {
    return Math.max(
      0,
      this.boardMembers.length - this.maxVisibleBoardMembers
    );
  }

  public onAddTask(): void {
    const modalRef = this.modalService.open(TaskModalComponent, TASK_MODAL_OPTIONS);

    modalRef.componentInstance.sprints = this.sprints;
    modalRef.componentInstance.members = this.boardMembers;
    modalRef.componentInstance.assignedUserId = null;
    modalRef.componentInstance.workItemId = null;
    this.setModalCurrentUser(modalRef.componentInstance);

    modalRef.result.then(
      (result) => {
        if (result?.title && this.boards?.length) {
          this.createTask(result);
        }
      },
      () => {}
    );
  }

  private getSelectedBoard(): BoardResponse | null {
    if (!this.boards?.length) {
      return null;
    }

    const selectedId = this.selectedBoardId();
    if (selectedId) {
      return this.boards.find((board) => board.id === selectedId) ?? this.boards[0];
    }

    return this.boards[0];
  }

  public onSelectBoard(board: BoardResponse): void {
    this.selectedBoardId.set(board.id);
    this.searchQuery.set('');
    this.selectedUser.set(null);
    this.loadBacklog(board.id);
  }

  public isBoardSelected(board: BoardResponse): boolean {
    return this.selectedBoardId() === board.id;
  }

  public onAddSprint(): void {
    const board = this.getSelectedBoard();
    if (!board) {
      return;
    }

    const modalRef = this.modalService.open(SprintModalComponent, {
      backdrop: 'static',
      keyboard: false,
    });

    modalRef.componentInstance.boardId = board.id;

    modalRef.result.then(
      () => {
        const board = this.getSelectedBoard();
        if (board) {
          this.loadBacklog(board.id);
        }
      },
      () => {}
    );
  }

  private refreshBacklog(): void {
    const board = this.getSelectedBoard();
    if (board) {
      this.loadBacklog(board.id);
    }
  }

  private createTask(result: {
    title: string;
    description: string;
    comments?: string[];
    pendingFiles?: File[];
    status: TaskStatus;
    points: number | null;
    sprintId?: number | null;
    assignedUserId?: number | null;
  }): void {
    const board = this.getSelectedBoard();
    if (!board) {
      return;
    }

    const request: WorkItemRequest = {
      name: result.title,
      description: result.description,
      boardId: board.id,
      priority: 'Medium',
      sprintId: result.sprintId ?? undefined,
      assignedUserId: result.assignedUserId,
    };

    this.taskService.create(request).subscribe({
      next: (createdTask) => {
        const comments = (result.comments ?? [])
          .map((content) => content.trim())
          .filter((content) => content.length > 0);
        const pendingFiles = result.pendingFiles ?? [];
        const runPostCreateActions = () => {
          const completeSave = () => {
            if (result.status !== createdTask.status) {
              this.taskService
                .changeStatus(createdTask.id, result.status)
                .subscribe({
                  next: () => {
                    this.closeTask(createdTask.id);
                    this.refreshBacklog();
                  },
                  error: (err) => {
                    console.error('Greška pri promeni statusa taska:', err);
                    this.showStatusError(err.error?.message ?? err.message);
                    this.closeTask(createdTask.id);
                    this.refreshBacklog();
                  },
                });
            } else {
              this.closeTask(createdTask.id);
              this.refreshBacklog();
            }
          };

          const uploads = [
            ...comments.map((content) =>
              this.commentService.create({
                workItemId: createdTask.id,
                content,
              })
            ),
            ...pendingFiles.map((file) =>
              this.workItemFileService.upload(createdTask.id, file)
            ),
          ];

          if (uploads.length > 0) {
            forkJoin(uploads).subscribe({
              next: () => completeSave(),
              error: (err) => {
                console.error('Greška pri čuvanju komentara/fajlova:', err);
                completeSave();
              },
            });
          } else {
            completeSave();
          }
        };

        this.taskService.openWorkItem(createdTask.id).subscribe({
          next: (lock) => {
            if (lock.mode !== 'WRITE') {
              this.refreshBacklog();
              return;
            }

            this.activeWorkItemId = createdTask.id;
            this.startHeartbeat(createdTask.id);
            runPostCreateActions();
          },
          error: (err) => {
            console.error('Greška pri zaključavanju novog taska:', err);
            this.refreshBacklog();
          },
        });
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

    const previousStatus = task.status;

    this.taskService.changeStatus(task.id, status).subscribe({
      next: (updated) => {
        task.status = updated.status;
        this.moveTaskInBoard(task, previousStatus, updated.status);
      },
      error: (err) => {
        console.error('Greška pri promeni statusa taska:', err);
        if (err.status === 403) {
          this.showTaskOccupiedToast();
          return;
        }
        this.showStatusError(err.error?.message ?? err.message);
      },
    });
  }

  private moveTaskInBoard(
    task: WorkItemResponse,
    fromStatus: TaskStatus,
    toStatus: TaskStatus
  ): void {
    if (fromStatus === toStatus) {
      return;
    }

    const taskInAll = this.allTasks.find((item) => item.id === task.id);
    if (taskInAll) {
      taskInAll.status = toStatus;
    }

    const fromList = this.boardTasksByStatus[fromStatus];
    const toList = this.boardTasksByStatus[toStatus];
    const index = fromList.findIndex((item) => item.id === task.id);

    if (index === -1) {
      return;
    }

    fromList.splice(index, 1);
    toList.push(task);
  }

  public onTaskAssigneeChange(
    task: WorkItemResponse,
    assignedUserId: number | null
  ): void {
    this.taskService.changeAssignee(task.id, assignedUserId).subscribe({
      next: (updated) => {
        task.assignedUserId = updated.assignedUserId;
        const taskInAll = this.allTasks.find((item) => item.id === task.id);
        if (taskInAll) {
          taskInAll.assignedUserId = updated.assignedUserId;
        }
        this.applyTaskFilters();
      },
      error: (err) => {
        console.error('Greška pri dodeli taska:', err);
      },
    });
  }

  public onTaskPriorityChange(
    task: WorkItemResponse,
    priority: TaskPriority
  ): void {
    this.taskService.changePriority(task.id, priority).subscribe({
      next: (updated) => {
        task.priority = updated.priority;
      },
      error: (err) => {
        console.error('Greška pri promeni prioriteta taska:', err);
      },
    });
  }

  private showTaskOccupiedToast(): void {
    this.stickyLockMessage = 'Task je trenutno zauzet.';
    this.showLockToast = true;

    if (this.lockToastTimer) {
      clearTimeout(this.lockToastTimer);
    }

    this.lockToastTimer = setTimeout(() => {
      this.showLockToast = false;
    }, 3000);
  }

  private startHeartbeat(workItemId: number): void {
    this.stopHeartbeat();
    this.heartbeatTimer = setInterval(() => {
      this.taskService.heartbeatWorkItem(workItemId).subscribe({
        error: (err) => {
          console.error('Greška pri heartbeat zaključavanja:', err);
          this.stopHeartbeat();
          if (this.activeWorkItemId === workItemId) {
            this.activeWorkItemId = null;
          }
        },
      });
    }, 15000);
  }

  private stopHeartbeat(): void {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer);
      this.heartbeatTimer = null;
    }
  }

  private showStatusError(message: string): void {
    this.statusErrorMessage =
      message || 'Invalid status transition.';
    this.showStatusErrorToast = true;
    setTimeout(() => {
      this.showStatusErrorToast = false;
    }, 3000);
  }

  public onToggleTaskOptions(taskId: number): void {
    this.taskMenuOpenId = this.taskMenuOpenId === taskId ? null : taskId;
  }

  public onOpenTask(task: WorkItemResponse): void {
    this.taskService.openWorkItem(task.id).subscribe({
      next: (lock) => {
        if (lock.mode === 'WRITE') {
          this.activeWorkItemId = task.id;
          this.startHeartbeat(task.id);
          this.openTaskModal(task, true);
          return;
        }

        this.showTaskOccupiedToast();
      },
      error: (err) => {
        console.error('Greška pri otvaranju taska:', err);
      },
    });
  }

  private setModalCurrentUser(modal: TaskModalComponent): void {
    if (!this.currentUser) {
      return;
    }

    modal.currentUserId = this.currentUser.id;
    modal.currentUserFirstName = this.currentUser.firstName;
    modal.currentUserLastName = this.currentUser.lastName;
  }

  private openTaskModal(task: WorkItemResponse, isEditMode: boolean): void {
    const modalRef = this.modalService.open(TaskModalComponent, TASK_MODAL_OPTIONS);

    modalRef.componentInstance.isEditMode = isEditMode;
    modalRef.componentInstance.workItemId = isEditMode ? task.id : null;
    modalRef.componentInstance.title = task.name;
    modalRef.componentInstance.description = task.description;
    modalRef.componentInstance.status = task.status;
    modalRef.componentInstance.points = null;
    modalRef.componentInstance.sprintId = task.sprintId ?? null;
    modalRef.componentInstance.members = this.boardMembers;
    modalRef.componentInstance.assignedUserId = task.assignedUserId ?? null;
    this.setModalCurrentUser(modalRef.componentInstance);

    modalRef.result.then(
      (result) => {
        if (result && isEditMode) {
          this.updateTask(
            task.id,
            result,
            task.boardId,
            task.status,
            task.priority,
            () => this.closeTask(task.id));
          return;
        }

        this.closeTask(task.id);
      },
      () => {
        this.closeTask(task.id);
      }
    );
  }

  private closeTask(taskId: number): void {
    this.stopHeartbeat();
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

  public onEditTaskFromMenu(
    task: WorkItemResponse,
    popover: NgbPopover,
    event: Event
  ): void {
    event.stopPropagation();
    popover.close();
    this.onEditTask(task);
  }

  public onDeleteTaskFromMenu(
    task: WorkItemResponse,
    popover: NgbPopover,
    event: Event
  ): void {
    event.stopPropagation();
    popover.close();
    this.onDeleteTask(task);
  }

  public onDeleteTask(task: WorkItemResponse): void {
    if (!confirm('Želite li sigurno da obrišete ovaj task?')) {
      return;
    }

    this.taskService.delete(task.id).subscribe({
      next: () => {
        this.refreshBacklog();
      },
      error: (err) => {
        console.error('Greška pri brisanju taska:', err);
        if (err.status === 403) {
          this.showTaskOccupiedToast();
        }
      },
    });
  }

  public onReleaseTask(task: WorkItemResponse): void {
    if (this.activeWorkItemId !== task.id) {
      return;
    }

    this.closeTask(task.id);
  }

  private updateTask(
    workItemId: number,
    result: {
      title: string;
      description: string;
      comment?: string;
      status: TaskStatus;
      points: number | null;
      sprintId?: number | null;
      assignedUserId?: number | null;
    },
    boardId: number,
    previousStatus: TaskStatus,
    priority: string,
    onComplete?: () => void
  ): void {
    const request: WorkItemRequest = {
      name: result.title,
      description: result.description,
      boardId,
      sprintId: result.sprintId ?? undefined,
      priority,
      assignedUserId: result.assignedUserId,
    };

    this.taskService.update(workItemId, request).subscribe({
      next: () => {
        const completeSave = () => {
          if (result.status !== previousStatus) {
            this.taskService
              .changeStatus(workItemId, result.status)
              .subscribe({
                next: () => {
                  this.refreshBacklog();
                  onComplete?.();
                },
                error: (err) => {
                  console.error('Greška pri promeni statusa taska:', err);
                  this.showStatusError(err.error?.message ?? err.message);
                  this.refreshBacklog();
                  onComplete?.();
                },
              });
          } else {
            this.refreshBacklog();
            onComplete?.();
          }
        };

        completeSave();
      },
      error: (err) => {
        console.error('Greška pri ažuriranju taska:', err);
        if (err.status === 403) {
          this.showTaskOccupiedToast();
        }
        onComplete?.();
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
        if (result.length > 0) {
          const selectedId = this.selectedBoardId();
          const board =
            selectedId !== null
              ? result.find((item) => item.id === selectedId) ?? result[0]
              : result[0];
          this.selectedBoardId.set(board.id);
          this.loadBacklog(board.id);
        } else {
          this.selectedBoardId.set(null);
          this.boardMembers = [];
          this.sprintTaskGroups = [];
          this.sprints = [];
        }
      },
      error: (err) => {
        console.error('Greška pri učitavanju tabli:', err);
      },
    });
  }

  public onHeaderGroupsToggle(group: SprintTaskGroup): void {
    const key = this.getGroupKey(group);
    this.collapsedGroupKeys.update((keys) => {
      const next = new Set(keys);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  }

  public isGroupExpanded(group: SprintTaskGroup): boolean {
    return !this.collapsedGroupKeys().has(this.getGroupKey(group));
  }

  private getGroupKey(group: SprintTaskGroup): string {
    return group.id === null ? 'backlog' : String(group.id);
  }

  public hasWorkspaceId = signal<boolean>(false);

  public onGetWorkspace() {
    this.hasWorkspaceId.set(true);
  }

  public onOpenCreateModal() {
    const modalRef = this.modalService.open(WorkspaceModalComponent, {});
    modalRef.componentInstance.onEmitOwnerId.subscribe(() => {
      modalRef.close();
      this.getWorkspace();
    });
  }

  public onOpenBoardInviteModal(): void {
    const board = this.getSelectedBoard();
    if (!board) {
      return;
    }

    const modalRef = this.modalService.open(InviteModalComponent, {
      backdrop: 'static',
      keyboard: true,
    });
    modalRef.componentInstance.boardId = board.id;
    modalRef.componentInstance.boardName = board.name;

    modalRef.result.then(
      () => {
        this.getBoardMembers(board.id);
      },
      () => {}
    );
  }

  public onOpenInviteModal() {
    const modalRef = this.modalService.open(InviteModalComponent, {
      backdrop: 'static',
      keyboard: true,
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
    const modalRef = this.modalService.open(BoardModalComponent, {
      backdrop: 'static',
      keyboard: true,
    });

    modalRef.result.then(
      () => {
        this.getBoards();
      },
      () => {}
    );
  }
}
