import { TaskPriority } from '../types/task-priority.type';

export interface UpdateSubWorkItemRequest {
  name: string;
  description: string;
  assignedUserId?: number | null;
  priority: TaskPriority;
}
