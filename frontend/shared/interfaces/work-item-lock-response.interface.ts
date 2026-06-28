export interface WorkItemLockResponse {
  workItemId: number;
  userId: number;
  mode: 'WRITE' | 'READ_ONLY' | 'UNLOCKED' | 'NO_LOCK';
  lockedAt?: string;
  expiresAt?: string;
}
