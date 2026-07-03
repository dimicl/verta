export interface WorkItemRequest {
  name: string;
  description: string;
  boardId: number;
  sprintId?: number;
  assignedUserId?: number | null;
  priority?: string;
}
