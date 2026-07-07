import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuditLogEntry } from '../../../../core/models/claim.model';
import { ClaimsService } from '../../../../core/services/claims.service';

@Component({
  selector: 'app-audit-log-tab',
  imports: [MatCardModule, MatPaginatorModule, MatProgressSpinnerModule, DatePipe],
  templateUrl: './audit-log-tab.html',
  styleUrl: './audit-log-tab.scss'
})
export class AuditLogTab implements OnInit {
  claimId = input.required<string>();

  private readonly claimsService = inject(ClaimsService);

  protected readonly entries = signal<AuditLogEntry[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(true);

  private page = 1;
  protected readonly pageSize = 20;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.claimsService.getAuditLog(this.claimId(), this.page, this.pageSize).subscribe({
      next: (result) => {
        this.entries.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.load();
  }
}
