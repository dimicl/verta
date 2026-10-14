import { WorkItemResponse } from '../../../shared/interfaces';

export interface SprintTaskGroup {
  id: number | null;
  name: string;
  tasks: WorkItemResponse[];
}
