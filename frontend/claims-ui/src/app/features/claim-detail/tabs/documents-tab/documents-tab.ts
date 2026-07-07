import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { environment } from '../../../../../environments/environment';
import { ClaimDocument } from '../../../../core/models/claim.model';
import { ClaimsService } from '../../../../core/services/claims.service';

@Component({
  selector: 'app-documents-tab',
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatTableModule, MatProgressSpinnerModule, DatePipe],
  templateUrl: './documents-tab.html',
  styleUrl: './documents-tab.scss'
})
export class DocumentsTab implements OnInit {
  claimId = input.required<string>();

  private readonly claimsService = inject(ClaimsService);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly documents = signal<ClaimDocument[]>([]);
  protected readonly loading = signal(true);
  protected readonly uploading = signal(false);
  protected readonly displayedColumns = ['documentName', 'documentType', 'uploadedAt', 'fileSizeBytes', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.claimsService.getDocuments(this.claimId()).subscribe({
      next: (docs) => {
        this.documents.set(docs);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploading.set(true);
    this.claimsService.uploadDocument(this.claimId(), file, 'Other').subscribe({
      next: () => {
        this.uploading.set(false);
        this.snackBar.open('Document uploaded.', 'OK', { duration: 3000 });
        input.value = '';
        this.load();
      },
      error: () => {
        this.uploading.set(false);
        input.value = '';
      }
    });
  }

  download(doc: ClaimDocument): void {
    window.open(this.resolveUrl(doc.downloadUrl), '_blank');
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  private resolveUrl(url: string): string {
    if (url.startsWith('http')) return url; // Azure Blob SAS URL - already absolute
    const origin = environment.apiBaseUrl.replace(/\/api\/?$/, '');
    return `${origin}${url}`;
  }
}
