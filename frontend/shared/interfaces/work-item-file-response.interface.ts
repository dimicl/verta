import { API_BASE_URL } from '../config/api.config';

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

export function resolveFileUrl(path: string): string {
  if (!path) {
    return '';
  }

  if (path.startsWith('blob:')) {
    return path;
  }

  let url = path;
  if (!(path.startsWith('http://') || path.startsWith('https://'))) {
    url = `${API_BASE_URL}${path.startsWith('/') ? path : `/${path}`}`;
  }

  if (url.includes('/api/work-item-files/') && url.includes('/content')) {
    const token = localStorage.getItem('token');
    if (token) {
      const separator = url.includes('?') ? '&' : '?';
      return `${url}${separator}access_token=${encodeURIComponent(token)}`;
    }
  }

  return url;
}

export function fileExtensionFromName(fileName: string): string {
  return fileName.split('.').pop()?.toLowerCase() ?? '';
}
