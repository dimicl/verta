import { UserRole } from '../types';

export interface WorkspaceMemberResponse {
  id: number;
  WorkspaceId: number;
  userId: number;
  ownerId: number;
  role: UserRole;
  createdAt: Date;
  firstName: string;
  lastName: string;
}
