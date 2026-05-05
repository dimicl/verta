import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-ve-extra-members',
  templateUrl: './ve-extra-members.component.html',
  styleUrl: './ve-extra-members.component.scss',
  imports: [CommonModule, NgbModule],
})
export class VeExtraMembersComponent {
  public onNotificationShown = signal<boolean>(false);
}
