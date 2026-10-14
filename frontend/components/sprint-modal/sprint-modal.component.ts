import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, signal } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { InputComponent } from '../input/input.component';
import { SprintService } from '../../shared/services';
import { SprintRequest, SprintResponse, UpdateSprintRequest } from '../../shared/interfaces';

@Component({
  selector: 'app-sprint-modal',
  imports: [CommonModule, InputComponent],
  templateUrl: './sprint-modal.component.html',
  styleUrl: './sprint-modal.component.scss',
})
export class SprintModalComponent implements OnInit {
  @Input() public boardId: number | null = null;
  @Input() public sprint: SprintResponse | null = null;

  public sprintName = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required],
  });

  public startDateValue = this.getDefaultStartDate();
  public endDateValue = this.getDefaultEndDate();
  public formError = signal('');
  public isSaving = signal(false);

  public get isEditMode(): boolean {
    return this.sprint != null;
  }

  constructor(
    private sprintService: SprintService,
    private activeModal: NgbActiveModal
  ) {}

  public ngOnInit(): void {
    if (!this.sprint) {
      return;
    }

    this.sprintName.setValue(this.sprint.name);
    this.startDateValue = this.toDateInput(this.sprint.startDate) || this.startDateValue;
    this.endDateValue = this.toDateInput(this.sprint.endDate) || this.endDateValue;
  }

  public onNameInput(value: string): void {
    this.sprintName.setValue(value);
    this.formError.set('');
  }

  public onStartDateInput(value: string): void {
    this.startDateValue = value;
    this.formError.set('');
  }

  public onEndDateInput(value: string): void {
    this.endDateValue = value;
    this.formError.set('');
  }

  public onSave(): void {
    if (!this.boardId || this.sprintName.invalid) {
      this.formError.set('Please enter a sprint name.');
      return;
    }

    if (!this.startDateValue || !this.endDateValue) {
      this.formError.set('Please select sprint start and end dates.');
      return;
    }

    if (this.endDateValue < this.startDateValue) {
      this.formError.set('End date must be on or after start date.');
      return;
    }

    this.formError.set('');
    this.isSaving.set(true);

    if (this.isEditMode && this.sprint) {
      const request: UpdateSprintRequest = {
        name: this.sprintName.value.trim(),
        startDate: this.toIsoDate(this.startDateValue),
        endDate: this.toIsoDate(this.endDateValue),
      };

      this.sprintService.update(this.sprint.id, request).subscribe({
        next: (result) => {
          this.isSaving.set(false);
          this.activeModal.close({ action: 'updated', sprint: result });
        },
        error: (err) => {
          this.isSaving.set(false);
          this.formError.set(
            err.error?.message ?? 'Sprint update failed. Please try again.'
          );
        },
      });
      return;
    }

    const request: SprintRequest = {
      name: this.sprintName.value.trim(),
      boardId: this.boardId,
      startDate: this.toIsoDate(this.startDateValue),
      endDate: this.toIsoDate(this.endDateValue),
    };

    this.sprintService.create(request).subscribe({
      next: (result) => {
        this.isSaving.set(false);
        this.activeModal.close({ action: 'created', sprint: result });
      },
      error: (err) => {
        this.isSaving.set(false);
        this.formError.set(
          err.error?.message ?? 'Sprint creation failed. Please try again.'
        );
      },
    });
  }

  public onDelete(): void {
    if (!this.sprint) {
      return;
    }

    if (!confirm(`Delete sprint "${this.sprint.name}"? Tasks stay on the board.`)) {
      return;
    }

    this.isSaving.set(true);
    this.sprintService.delete(this.sprint.id).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.activeModal.close({ action: 'deleted', sprintId: this.sprint!.id });
      },
      error: (err) => {
        this.isSaving.set(false);
        this.formError.set(
          err.error?.message ?? 'Sprint delete failed. Please try again.'
        );
      },
    });
  }

  public onClose(): void {
    this.activeModal.dismiss();
  }

  private getDefaultStartDate(): string {
    return this.formatDateInput(new Date());
  }

  private getDefaultEndDate(): string {
    const end = new Date();
    end.setDate(end.getDate() + 14);
    return this.formatDateInput(end);
  }

  private formatDateInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private toDateInput(value?: string | null): string {
    if (!value) {
      return '';
    }
    return value.slice(0, 10);
  }

  private toIsoDate(value: string): string {
    return `${value}T00:00:00.000Z`;
  }
}
