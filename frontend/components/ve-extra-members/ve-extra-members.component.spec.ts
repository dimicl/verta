import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeExtraMembersComponent } from './ve-extra-members.component';

describe('VeExtraMembersComponent', () => {
  let component: VeExtraMembersComponent;
  let fixture: ComponentFixture<VeExtraMembersComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeExtraMembersComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VeExtraMembersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
