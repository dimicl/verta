import { CommonModule } from '@angular/common';
import { Component, Input, signal } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { InputComponent } from '../input/input.component';
import { SprintService } from '../../shared/services';
import { SprintRequest } from '../../shared/interfaces';

@Component({
  selector: 'app-sprint-modal',
  imports: [CommonModule, InputComponent],
  templateUrl: './sprint-modal.component.html',
  styleUrl: './sprint-modal.component.scss',
})
export class SprintModalComponent {
  @Input() public boardId: number | null = null;

  public sprintName = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required],
  });

  public startDateValue = this.getDefaultStartDate();
  public endDateValue = this.getDefaultEndDate();
  public createError = signal('');

  constructor(
    private sprintService: SprintService,
    private activeModal: NgbActiveModal
  ) {}

  public onNameInput(value: string): void {
    this.sprintName.setValue(value);
    this.createError.set('');
  }

  public onStartDateInput(value: string): void {
    this.startDateValue = value;
    this.createError.set('');
  }

  public onEndDateInput(value: string): void {
    this.endDateValue = value;
    this.createError.set('');
  }

  public onCreate(): void {
    if (!this.boardId || this.sprintName.invalid) {
      this.createError.set('Please enter a sprint name.');
      return;
    }

    if (!this.startDateValue || !this.endDateValue) {
      this.createError.set('Please select sprint start and end dates.');
      return;
    }

    if (this.endDateValue < this.startDateValue) {
      this.createError.set('End date must be on or after start date.');
      return;
    }

    const request: SprintRequest = {
      name: this.sprintName.value.trim(),
      boardId: this.boardId,
      startDate: this.toIsoDate(this.startDateValue),
      endDate: this.toIsoDate(this.endDateValue),
    };

    this.createError.set('');

    this.sprintService.create(request).subscribe({
      next: (result) => {
        this.activeModal.close(result);
      },
      error: (err) => {
        this.createError.set(
          err.error?.message ?? 'Sprint creation failed. Please try again.'
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

  private toIsoDate(value: string): string {
    return `${value}T00:00:00.000Z`;
  }
}
