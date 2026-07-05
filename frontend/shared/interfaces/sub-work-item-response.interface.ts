import { TaskStatus } from '../types/task-status.type';
import { TaskPriority } from '../types/task-priority.type';

export interface SubWorkItemResponse {
  id: number;
  name: string;
  description: string;
  status: TaskStatus;
  priority: TaskPriority;
  workItemId: number;
  userId: number;
  assignedUserId?: number | null;
  createdAt: string;
  updatedAt?: string | null;
}
