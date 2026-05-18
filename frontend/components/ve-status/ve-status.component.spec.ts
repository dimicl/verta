import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeStatusComponent } from './ve-status.component';

describe('VeStatusComponent', () => {
  let component: VeStatusComponent;
  let fixture: ComponentFixture<VeStatusComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeStatusComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VeStatusComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
