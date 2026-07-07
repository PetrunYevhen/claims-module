import { Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface TextPromptData {
  title: string;
  label: string;
  required?: boolean;
}

@Component({
  selector: 'app-text-prompt-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>{{ data.label }}</mat-label>
        <textarea matInput rows="3" [formControl]="valueControl"></textarea>
        @if (data.required && valueControl.invalid && valueControl.touched) {
          <mat-error>This field is required.</mat-error>
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
export class TextPromptDialog {
  protected readonly data = inject<TextPromptData>(MAT_DIALOG_DATA);
  protected readonly dialogRef = inject(MatDialogRef<TextPromptDialog>);
  protected readonly valueControl = new FormControl('', { nonNullable: true });

  confirm(): void {
    if (this.data.required && !this.valueControl.value.trim()) {
      this.valueControl.markAsTouched();
      return;
    }
    this.dialogRef.close(this.valueControl.value);
  }
}
