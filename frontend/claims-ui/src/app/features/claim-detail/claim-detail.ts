import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { ActivatedRoute } from '@angular/router';
import { ClaimDetail, ClaimStatus } from '../../core/models/claim.model';
import { ClaimsService } from '../../core/services/claims.service';
import { StatusTransitionDialog } from '../../shared/status-transition-dialog/status-transition-dialog';
import { AuditLogTab } from './tabs/audit-log-tab/audit-log-tab';
import { DocumentsTab } from './tabs/documents-tab/documents-tab';
import { OverviewTab } from './tabs/overview-tab/overview-tab';
import { PartiesTab } from './tabs/parties-tab/parties-tab';
import { ReservesTab } from './tabs/reserves-tab/reserves-tab';

@Component({
  selector: 'app-claim-detail',
  imports: [
    MatTabsModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    DatePipe,
    OverviewTab,
    PartiesTab,
    ReservesTab,
    DocumentsTab,
    AuditLogTab
  ],
  templateUrl: './claim-detail.html',
  styleUrl: './claim-detail.scss'
})
export class ClaimDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly claimsService = inject(ClaimsService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly claimId = this.route.snapshot.paramMap.get('id')!;
  protected readonly claim = signal<ClaimDetail | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.claimsService.getById(this.claimId).subscribe({
      next: (claim) => {
        this.claim.set(claim);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  statusClass(status: string): string {
    return `status-${status.toLowerCase()}`;
  }

  transition(targetStatus: ClaimStatus): void {
    const requireReason = targetStatus === 'Withdrawn' || targetStatus === 'Reopened';
    const reasonLabel =
      targetStatus === 'Withdrawn'
        ? 'Withdrawal reason'
        : targetStatus === 'Reopened'
          ? 'Reopen reason'
          : targetStatus === 'Closed'
            ? 'Closure justification (required only if reserves are still open)'
            : 'Reason (optional)';
    const checklist =
      targetStatus === 'Closed'
        ? [
            'No reserve components with a pending approval',
            'At least one active Claimant party',
            'No unresolved Critical validation issues'
          ]
        : undefined;

    this.dialog
      .open(StatusTransitionDialog, { data: { targetStatus, requireReason, reasonLabel, checklist } })
      .afterClosed()
      .subscribe((reason: string | null) => {
        if (reason === null) return;
        this.claimsService.transitionStatus(this.claimId, targetStatus, reason || undefined).subscribe({
          next: (claim) => {
            this.claim.set(claim);
            this.snackBar.open(`Status changed to ${targetStatus}.`, 'OK', { duration: 4000 });
          }
        });
      });
  }

  saveNotes(notes: string): void {
    this.claimsService.updateNotes(this.claimId, notes).subscribe((claim) => {
      this.claim.set(claim);
      this.snackBar.open('Notes saved.', 'OK', { duration: 3000 });
    });
  }
}
