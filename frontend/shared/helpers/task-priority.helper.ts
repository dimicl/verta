import { TaskPriority } from '../types/task-priority.type';

export const TASK_PRIORITIES: TaskPriority[] = ['Low', 'Medium', 'High'];

export function normalizeTaskPriority(value: string | null | undefined): TaskPriority {
  const normalized = (value ?? 'Medium').trim().toLowerCase();

  if (normalized === 'low') {
    return 'Low';
  }

  if (normalized === 'high') {
    return 'High';
  }

  return 'Medium';
}
