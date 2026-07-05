export interface CommentRequest {
  workItemId: number;
  subWorkItemId?: number | null;
  content: string;
}
