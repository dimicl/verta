import { NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';

export const TASK_MODAL_OPTIONS: NgbModalOptions = {
  backdrop: 'static',
  keyboard: false,
  size: 'lg',
  scrollable: true,
};

export const STACKED_MODAL_OPTIONS: NgbModalOptions = {
  ...TASK_MODAL_OPTIONS,
  windowClass: 'stacked-modal',
  backdropClass: 'stacked-modal-backdrop',
};

export const CONFIRM_MODAL_OPTIONS: NgbModalOptions = {
  backdrop: 'static',
  keyboard: false,
  centered: true,
  size: 'sm',
  windowClass: 'confirm-modal',
  backdropClass: 'confirm-modal-backdrop',
};
