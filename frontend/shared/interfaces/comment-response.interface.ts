export interface CommentResponse {
  id: number;
  content: string;
  workItemId: number;
  userId: number;
  firstName: string;
  lastName: string;
  createdAt: string;
  updatedAt?: string;
}
