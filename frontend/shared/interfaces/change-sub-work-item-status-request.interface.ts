import { TaskStatus } from '../types/task-status.type';

export interface ChangeSubWorkItemStatusRequest {
  status: TaskStatus;
}
