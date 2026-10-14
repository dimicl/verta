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

export type WorkItemActivityType =
  | 'WorkItemUpdated'
  | 'CommentCreated'
  | 'WorkItemFileAdded'
  | 'WorkItemFileDeleted'
  | 'SubWorkItemUpdated'
  | 'SubWorkItemStatusChanged';

export interface WorkItemActivityNotificationPayload {
  activityType: WorkItemActivityType;
  workItemId: number;
  boardId: number;
  name: string;
  updatedByUserId: number;
  subWorkItemId?: number;
  status?: string;
  fileName?: string;
}
