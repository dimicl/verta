import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeProfileComponent } from './ve-profile.component';

describe('VeProfileComponent', () => {
  let component: VeProfileComponent;
  let fixture: ComponentFixture<VeProfileComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeProfileComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VeProfileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
