import { TaskStatus } from '../types/task-status.type';

export const STATUS_LABELS: Record<TaskStatus, string> = {
  ToDo: 'ToDo',
  InProgress: 'In Progress',
  PR: 'PR',
  Testing: 'Testing',
  Done: 'Done',
};

export const BOARD_COLUMNS: { label: string; status: TaskStatus }[] = (
  Object.entries(STATUS_LABELS) as [TaskStatus, string][]
).map(([status, label]) => ({ label, status }));

const transitions: Record<TaskStatus, TaskStatus[]> = {
  ToDo: ['InProgress'],
  InProgress: ['PR', 'ToDo'],
  PR: ['Testing', 'InProgress'],
  Testing: ['Done', 'InProgress'],
  Done: [],
};

export function getAllowedNextStatuses(current: TaskStatus): TaskStatus[] {
  return transitions[current] ?? [];
}
