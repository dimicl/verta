export interface WorkItemFileResponse {
  id: number;
  workItemId: number;
  fileName: string;
  fileType: string;
  fileSize: number;
  fileUrl: string;
  fileThumbnailUrl?: string | null;
  createdAt: string;
}

export const API_BASE_URL = 'http://localhost:8080';

export function resolveFileUrl(path: string): string {
  if (!path) {
    return '';
  }

  if (path.startsWith('http://') || path.startsWith('https://') || path.startsWith('blob:')) {
    return path;
  }

  return `${API_BASE_URL}${path.startsWith('/') ? path : `/${path}`}`;
}

export function fileExtensionFromName(fileName: string): string {
  return fileName.split('.').pop()?.toLowerCase() ?? '';
}
