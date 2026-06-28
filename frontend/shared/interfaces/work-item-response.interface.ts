import { TaskStatus } from '../types/task-status.type';

export interface WorkItemResponse {
  id: number;
  name: string;
  description: string;
  status: TaskStatus;
  priority: string;
  boardId: number;
  createdByUserId: number;
  assignedUserId?: number;
  createdAt: string;
  updatedAt?: string;
}
