export interface AppNotification {
  id: string;
  eventName: string;
  title: string;
  message: string;
  createdAt: Date;
  read: boolean;
  payload: Record<string, unknown>;
}

export interface UserPresencePayload {
  userId: number;
  isOnline: boolean;
}

export interface WorkItemAssignedPayload {
  workItemId: number;
  name: string;
  boardId: number;
  assignedByUserId: number;
}

export interface WorkItemStatusChangedPayload {
  workItemId: number;
  name: string;
  boardId: number;
  status: string;
  updatedByUserId: number;
}
