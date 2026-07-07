import { Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ClaimStatus } from '../../core/models/claim.model';

export interface TransitionDialogData {
  targetStatus: ClaimStatus;
  requireReason: boolean;
  reasonLabel: string;
  checklist?: string[];
}

@Component({
  selector: 'app-status-transition-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Transition to {{ data.targetStatus }}</h2>
    <mat-dialog-content>
      @if (data.checklist?.length) {
        <p>The following conditions must already be satisfied:</p>
        <ul>
          @for (item of data.checklist; track item) {
            <li>{{ item }}</li>
          }
        </ul>
      }
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>{{ data.reasonLabel }}</mat-label>
        <textarea matInput rows="3" [formControl]="reasonControl"></textarea>
        @if (data.requireReason && reasonControl.invalid && reasonControl.touched) {
          <mat-error>A reason is required for this transition.</mat-error>
        }
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close(null)">Cancel</button>
      <button mat-flat-button color="primary" (click)="confirm()">Confirm</button>
    </mat-dialog-actions>
  `,
  styles: [
    `
      .full-width {
        width: 100%;
        margin-top: 8px;
      }
    `
  ]
})
export class StatusTransitionDialog {
  protected readonly data = inject<TransitionDialogData>(MAT_DIALOG_DATA);
  protected readonly dialogRef = inject(MatDialogRef<StatusTransitionDialog>);
  protected readonly reasonControl = new FormControl('', { nonNullable: true });

  confirm(): void {
    if (this.data.requireReason && !this.reasonControl.value.trim()) {
      this.reasonControl.markAsTouched();
      return;
    }
    this.dialogRef.close(this.reasonControl.value);
  }
}
