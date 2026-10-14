export interface SprintRequest {
  name: string;
  boardId: number;
  startDate?: string;
  endDate?: string;
}

export interface UpdateSprintRequest {
  name: string;
  startDate?: string | null;
  endDate?: string | null;
}
