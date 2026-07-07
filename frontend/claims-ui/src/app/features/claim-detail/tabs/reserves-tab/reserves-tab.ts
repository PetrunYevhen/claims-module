import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { CreateReserveRequest, ReserveComponent, ReserveHistoryEntry } from '../../../../core/models/reserve.model';
import { AuthService } from '../../../../core/services/auth.service';
import { ReservesService } from '../../../../core/services/reserves.service';
import { TextPromptDialog } from '../../../../shared/text-prompt-dialog/text-prompt-dialog';
import { AddReserveDialog } from './add-reserve-dialog';

@Component({
  selector: 'app-reserves-tab',
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatTableModule, MatProgressSpinnerModule, DatePipe, CurrencyPipe],
  templateUrl: './reserves-tab.html',
  styleUrl: './reserves-tab.scss'
})
export class ReservesTab implements OnInit {
  claimId = input.required<string>();

  private readonly reservesService = inject(ReservesService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly components = signal<ReserveComponent[]>([]);
  protected readonly history = signal<ReserveHistoryEntry[]>([]);
  protected readonly totalApproved = signal(0);
  protected readonly loading = signal(true);

  protected readonly historyColumns = [
    'createdAt',
    'transactionType',
    'amount',
    'approvalStatus',
    'postingStatus',
    'actions'
  ];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.reservesService.get(this.claimId()).subscribe({
      next: (summary) => {
        this.components.set(summary.components);
        this.history.set(summary.history);
        this.totalApproved.set(summary.totalApproved);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private currentUserId(): string | undefined {
    return this.authService.currentUser()?.userId;
  }

  private currentRole(): string | undefined {
    return this.authService.currentUser()?.role;
  }

  canApproveOrReject(entry: ReserveHistoryEntry): boolean {
    const role = this.currentRole();
    return entry.approvalStatus === 'PendingApproval' && (role === 'supervisor' || role === 'manager');
  }

  canRetract(entry: ReserveHistoryEntry): boolean {
    return entry.approvalStatus === 'PendingApproval' && entry.submittedByUserId === this.currentUserId();
  }

  openAddReserveDialog(): void {
    this.dialog
      .open(AddReserveDialog)
      .afterClosed()
      .subscribe((request: CreateReserveRequest | undefined) => {
        if (!request) return;
        this.reservesService.create(this.claimId(), request).subscribe({
          next: (entry) => {
            const message = entry.approvalStatus === 'AutoApproved' ? 'Reserve auto-approved.' : 'Reserve submitted for approval.';
            this.snackBar.open(message, 'OK', { duration: 4000 });
            this.load();
          }
        });
      });
  }

  approve(entry: ReserveHistoryEntry): void {
    this.reservesService.approve(this.claimId(), entry.reserveHistoryId).subscribe(() => {
      this.snackBar.open('Reserve approved.', 'OK', { duration: 3000 });
      this.load();
    });
  }

  reject(entry: ReserveHistoryEntry): void {
    this.dialog
      .open(TextPromptDialog, { data: { title: 'Reject Reserve', label: 'Rejection reason', required: true } })
      .afterClosed()
      .subscribe((reason: string | null) => {
        if (!reason) return;
        this.reservesService.reject(this.claimId(), entry.reserveHistoryId, reason).subscribe(() => {
          this.snackBar.open('Reserve rejected.', 'OK', { duration: 3000 });
          this.load();
        });
      });
  }

  retract(entry: ReserveHistoryEntry): void {
    this.reservesService.retract(this.claimId(), entry.reserveHistoryId).subscribe(() => {
      this.snackBar.open('Reserve retracted.', 'OK', { duration: 3000 });
      this.load();
    });
  }

  statusClass(status: string): string {
    return `status-${status.toLowerCase()}`;
  }
}
