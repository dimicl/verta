import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import {
  BoardService,
  SignalRService,
  SprintService,
  SubWorkItemService,
  TaskService,
  UserService,
  WorkspaceService,
} from '../../../shared/services';
import { WorkspaceResponse } from '../../../shared/interfaces/workspace-response.interface';
import {
  BoardResponse,
  SprintResponse,
  SubWorkItemResponse,
  WorkItemResponse,
} from '../../../shared/interfaces';
import { WorkspaceMemberResponse } from '../../../shared/interfaces/workspace-member-response.interface';
import { UserRole } from '../../../shared/types/user-role.type';
import { TaskStatus } from '../../../shared/types/task-status.type';
import { TaskPriority } from '../../../shared/types/task-priority.type';
import {
  BOARD_COLUMNS,
  getAllowedNextStatuses,
} from '../../../shared/helpers/task-status.helper';
import {
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { SprintModalComponent } from '../../../components/sprint-modal/sprint-modal.component';
import { WorkspaceModalComponent } from '../../../components/workspace-modal/workspace-modal.component';
import { InviteModalComponent } from '../../../components/invite-modal/invite-modal.component';
import { BoardModalComponent } from '../../../components/board-modal/board-modal.component';
import { BoardMember, CurrentUser } from '../models/board-member.model';
import { SprintTaskGroup } from '../models/sprint-task-group.model';

@Injectable()
export class BoardWorkspaceStore {
  private readonly userService = inject(UserService);
  private readonly workspaceService = inject(WorkspaceService);
  private readonly boardService = inject(BoardService);
  private readonly sprintService = inject(SprintService);
  private readonly taskService = inject(TaskService);
  private readonly subWorkItemService = inject(SubWorkItemService);
  private readonly signalRService = inject(SignalRService);
  private readonly modalService = inject(NgbModal);
  private readonly router = inject(Router);

  private readonly maxVisibleBoardMembers = 5;
  private backlogRequestId = 0;
  private lockToastTimer: ReturnType<typeof setTimeout> | null = null;

  allTasks: WorkItemResponse[] = [];

  workspace: WorkspaceResponse | null = null;
  boards: BoardResponse[] | null = null;
  selectedBoardId = signal<number | null>(null);

  workspaceMembers: BoardMember[] = [];
  boardMembers: BoardMember[] = [];
  currentUser: CurrentUser | null = null;
  currentUserWorkspaceRole: UserRole | null = null;
  currentUserBoardRole: UserRole | null = null;

  sprintTaskGroups: SprintTaskGroup[] = [];
  sprints: SprintResponse[] = [];
  searchQuery = signal('');
  showStatusErrorToast = false;
  statusErrorMessage = '';

  readonly boardColumns = BOARD_COLUMNS;
  boardTasksByStatus: Record<TaskStatus, WorkItemResponse[]> = {
    ToDo: [],
    InProgress: [],
    PR: [],
    Testing: [],
    Done: [],
  };

  stickyLockMessage = '';
  collapsedGroupKeys = signal<Set<string>>(new Set());
  expandedTaskIds = signal<Set<number>>(new Set());
  showLockToast = false;

  subtasksByWorkItemId: Record<number, SubWorkItemResponse[]> = {};
  selectedUser = signal<number | null>(null);
  hasWorkspaceId = signal<boolean>(false);

  loadCurrentUser(): void {
    this.userService.getUserById().subscribe({
      next: (response: any) => {
        this.currentUser = {
          id: Number(response?.id ?? 0),
          firstName: response?.firstName ?? 'User',
          lastName: response?.lastName ?? '',
        };
        this.currentUserWorkspaceRole = this.resolveCurrentUserRole(
          this.workspaceMembers
        );
        this.currentUserBoardRole = this.resolveCurrentUserRole(
          this.boardMembers
        );
      },
      error: (err) => console.error('Greška pri učitavanju korisnika:', err),
    });
  }

  get effectiveRole(): UserRole | null {
    return this.currentUserBoardRole ?? this.currentUserWorkspaceRole;
  }

  get canManageWorkspace(): boolean {
    return this.currentUserWorkspaceRole === 'Owner';
  }

  get canEditBoard(): boolean {
    const role = this.effectiveRole;
    return role === 'Owner' || role === 'Member';
  }

  get boardColumnIds(): TaskStatus[] {
    return this.boardColumns.map((column) => column.status);
  }

  get visibleBoardMembers(): BoardMember[] {
    return this.boardMembers.slice(0, this.maxVisibleBoardMembers);
  }

  get hiddenBoardMembers(): BoardMember[] {
    return this.boardMembers.slice(this.maxVisibleBoardMembers);
  }

  get extraBoardMembersCount(): number {
    return Math.max(
      0,
      this.boardMembers.length - this.maxVisibleBoardMembers
    );
  }

  getWorkspace(): void {
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

  getBoards(): void {
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
          const replaceUrl =
            selectedId === null || board.id !== selectedId;
          this.applyBoardSelection(board, replaceUrl);
        } else {
          this.selectedBoardId.set(null);
          this.syncBoardToUrl(null, true);
          void this.signalRService.leaveBoard();
          this.boardMembers = [];
          this.currentUserBoardRole = null;
          this.sprintTaskGroups = [];
          this.sprints = [];
        }
      },
      error: (err) => {
        console.error('Greška pri učitavanju tabli:', err);
      },
    });
  }

  getSelectedBoard(): BoardResponse | null {
    if (!this.boards?.length) {
      return null;
    }

    const selectedId = this.selectedBoardId();
    if (selectedId) {
      return this.boards.find((board) => board.id === selectedId) ?? this.boards[0];
    }

    return this.boards[0];
  }

  onSelectBoard(board: BoardResponse): void {
    this.searchQuery.set('');
    this.selectedUser.set(null);
    this.applyBoardSelection(board);
  }

  private applyBoardSelection(
    board: BoardResponse,
    replaceUrl = false
  ): void {
    this.selectedBoardId.set(board.id);
    void this.signalRService.joinBoard(board.id);
    this.loadBacklog(board.id);
    this.syncBoardToUrl(board.id, replaceUrl);
  }

  private syncBoardToUrl(boardId: number | null, replaceUrl = false): void {
    const currentBoard = this.router.parseUrl(this.router.url).queryParams['board'];
    const nextBoard = boardId !== null ? String(boardId) : null;

    if (currentBoard === nextBoard) {
      return;
    }

    void this.router.navigate([], {
      queryParams: { board: boardId },
      queryParamsHandling: 'merge',
      replaceUrl,
    });
  }

  isBoardSelected(board: BoardResponse): boolean {
    return this.selectedBoardId() === board.id;
  }

  loadBacklog(boardId: number): void {
    const requestId = ++this.backlogRequestId;
    this.getBoardMembers(boardId, requestId);

    forkJoin({
      sprints: this.sprintService.getByBoardId(boardId),
      tasks: this.taskService.getByBoardId(boardId),
    }).subscribe({
      next: ({ sprints, tasks }) => {
        if (requestId !== this.backlogRequestId) {
          return;
        }

        this.sprints = sprints;
        this.allTasks = tasks;
        this.applyTaskFilters();
        this.loadSubtasksForTasks(tasks, requestId);
      },
      error: (err) => {
        if (requestId !== this.backlogRequestId) {
          return;
        }

        console.error('Greška pri učitavanju backloga:', err);
        this.sprintTaskGroups = [];
        this.sprints = [];
        this.allTasks = [];
        this.subtasksByWorkItemId = {};
        this.clearBoardTasks();
      },
    });
  }

  refreshBacklog(): void {
    const board = this.getSelectedBoard();
    if (board) {
      this.loadBacklog(board.id);
    }
  }

  onSearchChange(query: string): void {
    this.searchQuery.set(query);
    this.applyTaskFilters();
  }

  onUserSelect(userId: number): void {
    this.selectedUser.update((current) => (current === userId ? null : userId));
    this.applyTaskFilters();
  }

  hasActiveTaskFilters(): boolean {
    return (
      this.searchQuery().trim().length > 0 || this.selectedUser() !== null
    );
  }

  applyTaskFilters(): void {
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

  onBoardDrop(
    event: CdkDragDrop<WorkItemResponse[]>,
    targetStatus: TaskStatus
  ): void {
    if (!this.canEditBoard) {
      return;
    }
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

  onTaskStatusChange(task: WorkItemResponse, status: TaskStatus): void {
    if (!this.canEditBoard) {
      return;
    }
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

  onTaskAssigneeChange(
    task: WorkItemResponse,
    assignedUserId: number | null
  ): void {
    if (!this.canEditBoard) {
      return;
    }
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

  onTaskPriorityChange(
    task: WorkItemResponse,
    priority: TaskPriority
  ): void {
    if (!this.canEditBoard) {
      return;
    }
    this.taskService.changePriority(task.id, priority).subscribe({
      next: (updated) => {
        task.priority = updated.priority;
      },
      error: (err) => {
        console.error('Greška pri promeni prioriteta taska:', err);
      },
    });
  }

  onSubtaskStatusChange(
    taskId: number,
    subtask: SubWorkItemResponse,
    status: TaskStatus
  ): void {
    if (!this.canEditBoard) {
      return;
    }
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

  hasSubtasks(taskId: number): boolean {
    return (this.subtasksByWorkItemId[taskId]?.length ?? 0) > 0;
  }

  getSubtasks(taskId: number): SubWorkItemResponse[] {
    return this.subtasksByWorkItemId[taskId] ?? [];
  }

  isTaskExpanded(taskId: number): boolean {
    return this.expandedTaskIds().has(taskId);
  }

  onToggleTaskSubtasks(taskId: number, event: Event): void {
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

  onHeaderGroupsToggle(group: SprintTaskGroup): void {
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

  isGroupExpanded(group: SprintTaskGroup): boolean {
    return !this.collapsedGroupKeys().has(this.getGroupKey(group));
  }

  showTaskOccupiedToast(): void {
    this.stickyLockMessage = 'Task je trenutno zauzet.';
    this.showLockToast = true;

    if (this.lockToastTimer) {
      clearTimeout(this.lockToastTimer);
    }

    this.lockToastTimer = setTimeout(() => {
      this.showLockToast = false;
    }, 3000);
  }

  showStatusError(message: string): void {
    this.statusErrorMessage =
      message || 'Invalid status transition.';
    this.showStatusErrorToast = true;
    setTimeout(() => {
      this.showStatusErrorToast = false;
    }, 3000);
  }

  clearLockToastTimer(): void {
    if (this.lockToastTimer) {
      clearTimeout(this.lockToastTimer);
      this.lockToastTimer = null;
    }
  }

  onAddSprint(): void {
    if (!this.canEditBoard) {
      return;
    }
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
        this.refreshBacklog();
      },
      () => {}
    );
  }

  onEditSprint(group: SprintTaskGroup, event: Event): void {
    event.stopPropagation();
    if (!this.canEditBoard || group.id === null) {
      return;
    }

    const board = this.getSelectedBoard();
    const sprint = this.sprints.find((item) => item.id === group.id);
    if (!board || !sprint) {
      return;
    }

    const modalRef = this.modalService.open(SprintModalComponent, {
      backdrop: 'static',
      keyboard: false,
    });

    modalRef.componentInstance.boardId = board.id;
    modalRef.componentInstance.sprint = sprint;

    modalRef.result.then(
      () => {
        this.refreshBacklog();
      },
      () => {}
    );
  }

  onGetWorkspace(): void {
    this.hasWorkspaceId.set(true);
  }

  onOpenCreateModal(): void {
    const modalRef = this.modalService.open(WorkspaceModalComponent, {});
    modalRef.componentInstance.mode = 'create';
    modalRef.componentInstance.onEmitOwnerId.subscribe(() => {
      modalRef.close();
      this.getWorkspace();
    });
  }

  onOpenWorkspaceSettings(): void {
    if (!this.canManageWorkspace || !this.workspace) {
      return;
    }

    const modalRef = this.modalService.open(WorkspaceModalComponent, {
      backdrop: 'static',
      keyboard: true,
    });
    modalRef.componentInstance.mode = 'edit';
    modalRef.componentInstance.workspace = this.workspace;

    modalRef.result.then(
      (result) => {
        if (result?.action === 'deleted') {
          this.workspace = null;
          this.boards = [];
          this.sprintTaskGroups = [];
          this.sprints = [];
          this.selectedBoardId.set(null);
          this.syncBoardToUrl(null, true);
          return;
        }
        this.getWorkspace();
      },
      () => {}
    );
  }

  onOpenBoardInviteModal(): void {
    if (!this.canManageWorkspace) {
      return;
    }
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

  onOpenInviteModal(): void {
    if (!this.canManageWorkspace) {
      return;
    }
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

  onAddBoard(): void {
    if (!this.canManageWorkspace) {
      return;
    }
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

  getBoardMembers(boardId: number, requestId: number = this.backlogRequestId): void {
    this.boardService.getBoardMembers(boardId).subscribe({
      next: (members) => {
        if (requestId !== this.backlogRequestId || this.selectedBoardId() !== boardId) {
          return;
        }
        this.boardMembers = this.mapMembers(members);
        this.currentUserBoardRole = this.resolveCurrentUserRole(
          this.boardMembers
        );
      },
      error: (err) => {
        if (requestId !== this.backlogRequestId || this.selectedBoardId() !== boardId) {
          return;
        }
        console.error('Greška pri učitavanju članova table:', err);
        this.boardMembers = [];
        this.currentUserBoardRole = null;
      },
    });
  }

  private getWorkspaceMembers(workspaceId: number): void {
    this.workspaceService.getMembers(workspaceId).subscribe({
      next: (members) => {
        this.workspaceMembers = this.mapMembers(members);
        this.currentUserWorkspaceRole = this.resolveCurrentUserRole(
          this.workspaceMembers
        );
      },
      error: (err) => {
        console.error('Greška pri učitavanju članova radnog prostora:', err);
      },
    });
  }

  mapMembers(members: WorkspaceMemberResponse[]): BoardMember[] {
    return members.map((member: any) => ({
      id: Number(member.userId ?? member.UserId ?? 0),
      firstName: member.firstName || member.FirstName || 'User',
      lastName: member.lastName || member.LastName || '',
      role: this.normalizeRole(member.role ?? member.Role),
    }));
  }

  normalizeRole(role: string | number | UserRole | undefined): UserRole {
    if (role === 'Owner' || role === 0 || role === '0') {
      return 'Owner';
    }
    if (
      role === 'Member' ||
      role === 'Guest' ||
      role === 'Viewer' ||
      role === 1 ||
      role === '1' ||
      role === 2 ||
      role === '2'
    ) {
      return 'Member';
    }
    return 'Member';
  }

  private resolveCurrentUserRole(
    members: Array<{ id: number; role: UserRole }>
  ): UserRole | null {
    if (!this.currentUser?.id) {
      return null;
    }
    return members.find((m) => m.id === this.currentUser!.id)?.role ?? null;
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

  private clearBoardTasks(): void {
    this.boardTasksByStatus = {
      ToDo: [],
      InProgress: [],
      PR: [],
      Testing: [],
      Done: [],
    };
  }

  private loadSubtasksForTasks(
    tasks: WorkItemResponse[],
    requestId: number = this.backlogRequestId
  ): void {
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
        if (requestId !== this.backlogRequestId) {
          return;
        }
        const map: Record<number, SubWorkItemResponse[]> = {};
        tasks.forEach((task, index) => {
          map[task.id] = results[index];
        });
        this.subtasksByWorkItemId = map;
      },
      error: (err) => {
        if (requestId !== this.backlogRequestId) {
          return;
        }
        console.error('Greška pri učitavanju podzadataka:', err);
        this.subtasksByWorkItemId = {};
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

  private getGroupKey(group: SprintTaskGroup): string {
    return group.id === null ? 'backlog' : String(group.id);
  }
}
