import { UserRole } from '../types/user-role.type';

/** Shared person shape for assignees / avatars (no role). */
export interface TaskMember {
  id: number;
  firstName: string;
  lastName: string;
}

/** Board/workspace member with role. */
export interface BoardMember extends TaskMember {
  role: UserRole;
}

export interface CurrentUser {
  id: number;
  firstName: string;
  lastName: string;
}
