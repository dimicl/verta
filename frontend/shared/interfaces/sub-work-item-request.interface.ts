import { TaskPriority } from '../types/task-priority.type';

export interface SubWorkItemRequest {
  name: string;
  description: string;
  workItemId: number;
  assignedUserId?: number | null;
  priority?: TaskPriority;
}
