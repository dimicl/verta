import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TaskModalComponent } from './task-modal.component';

describe('TaskModalComponent', () => {
  let component: TaskModalComponent;
  let fixture: ComponentFixture<TaskModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TaskModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should include the comment in the save payload', () => {
    component.title = 'Task title';
    component.description = 'Task description';
    component.comment = 'Please review this task';

    const closeSpy = spyOn((component as any).activeModal, 'close');

    component.onSave();

    expect(closeSpy).toHaveBeenCalledWith(jasmine.objectContaining({
      comment: 'Please review this task',
    }));
  });
});
