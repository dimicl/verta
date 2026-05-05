import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeNotificationComponent } from './ve-notification.component';

describe('VeNotificationComponent', () => {
  let component: VeNotificationComponent;
  let fixture: ComponentFixture<VeNotificationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeNotificationComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VeNotificationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
