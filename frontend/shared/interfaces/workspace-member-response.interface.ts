import { UserRole } from '../types';

export interface WorkspaceMemberResponse {
  id: number;
  WorkspaceId: number;
  ownerId: number;
  role: UserRole;
  createdAt: Date;
}
