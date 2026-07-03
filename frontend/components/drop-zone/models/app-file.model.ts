export type FileExtension =
  | 'pdf'
  | 'png'
  | 'jpg'
  | 'jpeg'
  | 'gif'
  | 'avi'
  | 'mov'
  | 'mp4';

export interface AppFile {
  fileId: number;
  fileName: string;
  type: FileExtension;
  size: number;
  imagePreviewUrl: string;
  file?: File | null;
  pending?: boolean;
}
