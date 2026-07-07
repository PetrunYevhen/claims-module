import { Component, inject, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ClaimDetail, ClaimParty, CreatePartyRequest } from '../../../../core/models/claim.model';
import { ClaimsService } from '../../../../core/services/claims.service';
import { AddPartyDialog } from './add-party-dialog';

@Component({
  selector: 'app-parties-tab',
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './parties-tab.html',
  styleUrl: './parties-tab.scss'
})
export class PartiesTab {
  claim = input.required<ClaimDetail>();
  refresh = output<void>();

  private readonly claimsService = inject(ClaimsService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  isLastActiveClaimant(party: ClaimParty): boolean {
    if (party.partyRole !== 'Claimant' || !party.isActive) return false;
    const activeClaimants = this.claim().parties.filter((p) => p.partyRole === 'Claimant' && p.isActive);
    return activeClaimants.length === 1;
  }

  openAddPartyDialog(): void {
    this.dialog
      .open(AddPartyDialog)
      .afterClosed()
      .subscribe((request: CreatePartyRequest | undefined) => {
        if (!request) return;
        this.claimsService.addParty(this.claim().claimId, request).subscribe(() => {
          this.snackBar.open('Party added.', 'OK', { duration: 3000 });
          this.refresh.emit();
        });
      });
  }

  removeParty(party: ClaimParty): void {
    this.claimsService.removeParty(this.claim().claimId, party.claimPartyId).subscribe(() => {
      this.snackBar.open('Party removed.', 'OK', { duration: 3000 });
      this.refresh.emit();
    });
  }
}
