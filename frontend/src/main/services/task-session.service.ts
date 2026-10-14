import { Injectable, inject } from '@angular/core';
import { forkJoin } from 'rxjs';
import {
  CommentService,
  SubWorkItemService,
  TaskService,
  WorkItemFileService,
} from '../../../shared/services';
import { WorkItemRequest, WorkItemResponse } from '../../../shared/interfaces';
import { SubWorkItemResponse } from '../../../shared/interfaces';
import { NgbModal, NgbModalRef, NgbPopover } from '@ng-bootstrap/ng-bootstrap';
import { TaskModalComponent } from '../../../components/task-modal/task-modal.component';
import { STACKED_MODAL_OPTIONS, TASK_MODAL_OPTIONS } from '../../../shared/constants/modal-options';
import { TaskStatus } from '../../../shared/types/task-status.type';
import { TaskPriority } from '../../../shared/types/task-priority.type';
import { BoardWorkspaceStore } from './board-workspace.store';
import { ActiveWorkItemLockTracker } from '../../../shared/services/active-work-item-lock.tracker';

@Injectable()
export class TaskSessionService {
  private readonly store = inject(BoardWorkspaceStore);
  private readonly taskService = inject(TaskService);
  private readonly commentService = inject(CommentService);
  private readonly subWorkItemService = inject(SubWorkItemService);
  private readonly workItemFileService = inject(WorkItemFileService);
  private readonly modalService = inject(NgbModal);

  activeWorkItemId: number | null = null;
  openTaskModalRef: NgbModalRef | null = null;
  taskMenuOpenId: number | null = null;

  private heartbeatTimer: ReturnType<typeof setInterval> | null = null;
  private readonly lockTracker = inject(ActiveWorkItemLockTracker);

  onAddTask(): void {
    if (!this.store.canEditBoard) {
      return;
    }
    const modalRef = this.modalService.open(TaskModalComponent, TASK_MODAL_OPTIONS);

    modalRef.componentInstance.sprints = this.store.sprints;
    modalRef.componentInstance.members = this.store.boardMembers;
    modalRef.componentInstance.assignedUserId = null;
    modalRef.componentInstance.workItemId = null;
    this.setModalCurrentUser(modalRef.componentInstance);

    modalRef.result.then(
      (result) => {
        if (result?.title && this.store.boards?.length) {
          this.createTask(result);
        }
      },
      () => {}
    );
  }

  onOpenTask(task: WorkItemResponse): void {
    this.taskService.openWorkItem(task.id).subscribe({
      next: (lock) => {
        if (lock.mode === 'WRITE') {
          this.setActiveWorkItem(task.id);
          this.startHeartbeat(task.id);
          this.openTaskModal(task, true, false);
          return;
        }

        if (lock.mode === 'READ_ONLY') {
          this.setActiveWorkItem(task.id);
          this.openTaskModal(task, true, true);
          return;
        }

        this.store.showTaskOccupiedToast();
      },
      error: (err) => {
        console.error('Greška pri otvaranju taska:', err);
      },
    });
  }

  onEditTask(task: WorkItemResponse): void {
    this.onOpenTask(task);
  }

  onEditTaskFromMenu(
    task: WorkItemResponse,
    popover: NgbPopover,
    event: Event
  ): void {
    event.stopPropagation();
    popover.close();
    this.onEditTask(task);
  }

  onDeleteTaskFromMenu(
    task: WorkItemResponse,
    popover: NgbPopover,
    event: Event
  ): void {
    event.stopPropagation();
    popover.close();
    this.onDeleteTask(task);
  }

  onDeleteTask(task: WorkItemResponse): void {
    if (!this.store.canEditBoard) {
      return;
    }
    if (!confirm('Želite li sigurno da obrišete ovaj task?')) {
      return;
    }

    this.taskService.delete(task.id).subscribe({
      next: () => {
        this.store.refreshBacklog();
      },
      error: (err) => {
        console.error('Greška pri brisanju taska:', err);
        if (err.status === 403) {
          this.store.showTaskOccupiedToast();
        }
      },
    });
  }

  onReleaseTask(task: WorkItemResponse): void {
    if (this.activeWorkItemId !== task.id) {
      return;
    }

    this.closeTask(task.id);
  }

  onOpenSubtask(task: WorkItemResponse, subtask: SubWorkItemResponse): void {
    const modalRef = this.modalService.open(TaskModalComponent, STACKED_MODAL_OPTIONS);
    const roleReadOnly = !this.store.canEditBoard;

    modalRef.componentInstance.isSubtask = true;
    modalRef.componentInstance.isEditMode = true;
    modalRef.componentInstance.isReadOnly = roleReadOnly;
    modalRef.componentInstance.parentWorkItemId = task.id;
    modalRef.componentInstance.parentTaskTitle = task.name;
    modalRef.componentInstance.subWorkItemId = subtask.id;
    modalRef.componentInstance.title = subtask.name;
    modalRef.componentInstance.description = subtask.description;
    modalRef.componentInstance.status = subtask.status;
    modalRef.componentInstance.priority = subtask.priority ?? 'Medium';
    modalRef.componentInstance.assignedUserId = subtask.assignedUserId ?? null;
    modalRef.componentInstance.members = this.store.boardMembers;
    this.setModalCurrentUser(modalRef.componentInstance);

    modalRef.result.then(
      (result) => {
        if (result?.title && !roleReadOnly) {
          this.saveSubtaskFromBoard(task, subtask, result);
        }
      },
      () => {}
    );
  }

  onToggleTaskOptions(taskId: number): void {
    this.taskMenuOpenId = this.taskMenuOpenId === taskId ? null : taskId;
  }

  startHeartbeat(workItemId: number): void {
    this.stopHeartbeat();
    this.heartbeatTimer = setInterval(() => {
      this.taskService.heartbeatWorkItem(workItemId).subscribe({
        error: (err) => {
          console.error('Greška pri heartbeat zaključavanja:', err);
          this.stopHeartbeat();
          if (this.activeWorkItemId === workItemId) {
            this.setActiveWorkItem(null);
          }
        },
      });
    }, 15000);
  }

  stopHeartbeat(): void {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer);
      this.heartbeatTimer = null;
    }
  }

  destroySession(): void {
    this.stopHeartbeat();
    if (this.openTaskModalRef) {
      try {
        this.openTaskModalRef.dismiss();
      } catch {
        // modal already closed
      }
      this.openTaskModalRef = null;
    }
    if (this.activeWorkItemId !== null) {
      const id = this.activeWorkItemId;
      this.setActiveWorkItem(null);
      this.taskService.closeWorkItem(id).subscribe({ error: () => {} });
    }
  }

  private setActiveWorkItem(workItemId: number | null): void {
    this.activeWorkItemId = workItemId;
    this.lockTracker.set(workItemId);
  }

  closeTask(taskId: number): void {
    this.stopHeartbeat();
    this.taskService.closeWorkItem(taskId).subscribe({
      next: () => {
        if (this.activeWorkItemId === taskId) {
          this.setActiveWorkItem(null);
        }
      },
      error: (err) => {
        console.error('Greška pri zatvaranju task zaključavanja:', err);
      },
    });
  }

  setModalCurrentUser(modal: TaskModalComponent): void {
    if (!this.store.currentUser) {
      return;
    }

    modal.currentUserId = this.store.currentUser.id;
    modal.currentUserFirstName = this.store.currentUser.firstName;
    modal.currentUserLastName = this.store.currentUser.lastName;
  }

  openTaskModal(
    task: WorkItemResponse,
    isEditMode: boolean,
    isReadOnly = false
  ): void {
    const modalRef = this.modalService.open(TaskModalComponent, TASK_MODAL_OPTIONS);
    this.openTaskModalRef = modalRef;

    modalRef.componentInstance.isEditMode = isEditMode;
    modalRef.componentInstance.isReadOnly = isReadOnly;
    modalRef.componentInstance.workItemId = isEditMode ? task.id : null;
    modalRef.componentInstance.boardId = task.boardId;
    modalRef.componentInstance.title = task.name;
    modalRef.componentInstance.description = task.description;
    modalRef.componentInstance.status = task.status;
    modalRef.componentInstance.priority = (task.priority as TaskPriority) ?? 'Medium';
    modalRef.componentInstance.points = null;
    modalRef.componentInstance.sprintId = task.sprintId ?? null;
    modalRef.componentInstance.members = this.store.boardMembers;
    modalRef.componentInstance.assignedUserId = task.assignedUserId ?? null;
    this.setModalCurrentUser(modalRef.componentInstance);

    modalRef.result.then(
      (result) => {
        this.openTaskModalRef = null;
        if (result && isEditMode && !modalRef.componentInstance.isReadOnly) {
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
        this.openTaskModalRef = null;
        this.closeTask(task.id);
      }
    );
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
    const board = this.store.getSelectedBoard();
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
                    this.store.refreshBacklog();
                  },
                  error: (err) => {
                    console.error('Greška pri promeni statusa taska:', err);
                    this.store.showStatusError(err.error?.message ?? err.message);
                    this.closeTask(createdTask.id);
                    this.store.refreshBacklog();
                  },
                });
            } else {
              this.closeTask(createdTask.id);
              this.store.refreshBacklog();
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
              this.store.refreshBacklog();
              return;
            }

            this.setActiveWorkItem(createdTask.id);
            this.startHeartbeat(createdTask.id);
            runPostCreateActions();
          },
          error: (err) => {
            console.error('Greška pri zaključavanju novog taska:', err);
            this.store.refreshBacklog();
          },
        });
      },
      error: (err) => {
        console.error('Greška pri kreiranju taska:', err);
      },
    });
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
                  this.store.refreshBacklog();
                  onComplete?.();
                },
                error: (err) => {
                  console.error('Greška pri promeni statusa taska:', err);
                  this.store.showStatusError(err.error?.message ?? err.message);
                  this.store.refreshBacklog();
                  onComplete?.();
                },
              });
          } else {
            this.store.refreshBacklog();
            onComplete?.();
          }
        };

        completeSave();
      },
      error: (err) => {
        console.error('Greška pri ažuriranju taska:', err);
        if (err.status === 403) {
          this.store.showTaskOccupiedToast();
        }
        onComplete?.();
      },
    });
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
            const list = this.store.subtasksByWorkItemId[task.id];
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
          this.store.showStatusError(err.error?.message ?? err.message);
        },
      });
  }
}
