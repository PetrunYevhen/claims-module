import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, effect, input, output } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ClaimDetail } from '../../../../core/models/claim.model';

@Component({
  selector: 'app-overview-tab',
  imports: [ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, DatePipe, CurrencyPipe],
  templateUrl: './overview-tab.html',
  styleUrl: './overview-tab.scss'
})
export class OverviewTab {
  claim = input.required<ClaimDetail>();
  save = output<string>();

  protected readonly notesControl = new FormControl('', { nonNullable: true });
  private lastLoadedClaimId: string | null = null;

  constructor() {
    effect(() => {
      const claim = this.claim();
      if (claim.claimId !== this.lastLoadedClaimId) {
        this.notesControl.setValue(claim.notes ?? '');
        this.lastLoadedClaimId = claim.claimId;
      }
    });
  }

  saveNotes(): void {
    this.save.emit(this.notesControl.value);
  }
}
