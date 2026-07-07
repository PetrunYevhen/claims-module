import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Router } from '@angular/router';
import { ClaimsService } from '../../core/services/claims.service';
import { ReferenceDataService } from '../../core/services/reference-data.service';
import { ClaimStatus, ClaimSummary } from '../../core/models/claim.model';
import { CauseOfLossCode } from '../../core/models/reference.model';

const STATUS_OPTIONS: ClaimStatus[] = [
  'Draft',
  'Open',
  'UnderInvestigation',
  'PendingPayment',
  'Closed',
  'Reopened',
  'Withdrawn'
];

@Component({
  selector: 'app-claims-list',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTableModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    DatePipe,
    CurrencyPipe
  ],
  templateUrl: './claims-list.html',
  styleUrl: './claims-list.scss'
})
export class ClaimsList implements OnInit {
  private readonly claimsService = inject(ClaimsService);
  private readonly referenceDataService = inject(ReferenceDataService);
  private readonly router = inject(Router);

  protected readonly claims = signal<ClaimSummary[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(false);
  protected readonly causeOfLossCodes = signal<CauseOfLossCode[]>([]);

  protected readonly statusOptions = STATUS_OPTIONS;
  protected readonly displayedColumns = [
    'claimNumber',
    'policyNumber',
    'clientName',
    'lossDate',
    'causeOfLossCode',
    'status',
    'totalReserves'
  ];

  protected readonly filterForm = new FormGroup({
    statuses: new FormControl<ClaimStatus[]>([], { nonNullable: true }),
    dateFrom: new FormControl<Date | null>(null),
    dateTo: new FormControl<Date | null>(null),
    causeOfLossCode: new FormControl<string>('', { nonNullable: true }),
    search: new FormControl<string>('', { nonNullable: true })
  });

  private page = 1;
  protected pageSize = 20;

  ngOnInit(): void {
    this.referenceDataService.getCauseOfLossCodes().subscribe((codes) => this.causeOfLossCodes.set(codes));
    this.load();
  }

  load(): void {
    this.loading.set(true);
    const value = this.filterForm.getRawValue();

    this.claimsService
      .list({
        statuses: value.statuses.length ? value.statuses : undefined,
        dateFrom: value.dateFrom ? value.dateFrom.toISOString() : undefined,
        dateTo: value.dateTo ? value.dateTo.toISOString() : undefined,
        causeOfLossCode: value.causeOfLossCode || undefined,
        search: value.search || undefined,
        page: this.page,
        pageSize: this.pageSize
      })
      .subscribe({
        next: (result) => {
          this.claims.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  applyFilters(): void {
    this.page = 1;
    this.load();
  }

  resetFilters(): void {
    this.filterForm.reset({ statuses: [], dateFrom: null, dateTo: null, causeOfLossCode: '', search: '' });
    this.applyFilters();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.load();
  }

  openClaim(claim: ClaimSummary): void {
    this.router.navigate(['/claims', claim.claimId]);
  }

  createClaim(): void {
    this.router.navigate(['/claims/new']);
  }

  statusClass(status: ClaimStatus): string {
    return `status-${status.toLowerCase()}`;
  }
}
