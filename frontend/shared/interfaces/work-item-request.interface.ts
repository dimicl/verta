export interface WorkItemRequest {
  name: string;
  description: string;
  boardId: number;
  assignedUserId?: number;
  priority?: string;
}
