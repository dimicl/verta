import { TaskStatus } from '../types/task-status.type';

export interface ChangeWorkItemStatusRequest {
  status: TaskStatus;
}
