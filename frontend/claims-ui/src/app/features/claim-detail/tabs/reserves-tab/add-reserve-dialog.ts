import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { CreateReserveRequest, ReserveComponentType } from '../../../../core/models/reserve.model';

const COMPONENTS: ReserveComponentType[] = ['Indemnity', 'Expense', 'ALAE', 'SubrogationRecoverable'];

@Component({
  selector: 'app-add-reserve-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './add-reserve-dialog.html',
  styles: [
    `
      .dialog-form {
        display: flex;
        flex-direction: column;
        gap: 4px;
        min-width: 360px;
      }
      .full-width {
        width: 100%;
      }
      .authority-indicator {
        display: flex;
        align-items: center;
        gap: 6px;
        font-weight: 600;
        padding: 8px 12px;
        border-radius: 8px;
        width: fit-content;
        margin-top: 4px;
      }
      .auth-auto {
        color: #2e7d32;
        background: rgba(67, 160, 71, 0.12);
      }
      .auth-warn {
        color: #b26a00;
        background: rgba(255, 179, 0, 0.15);
      }
    `
  ]
})
export class AddReserveDialog {
  private readonly dialogRef = inject(MatDialogRef<AddReserveDialog>);
  protected readonly components = COMPONENTS;

  protected readonly form = new FormGroup({
    component: new FormControl<ReserveComponentType>('Indemnity', { nonNullable: true, validators: [Validators.required] }),
    amount: new FormControl<number | null>(null, [Validators.required]),
    changeReason: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  authorityIndicator(): { label: string; icon: string; cssClass: string } | null {
    const amount = this.form.controls.amount.value;
    if (amount === null || amount === undefined || amount === 0) return null;

    const magnitude = Math.abs(amount);
    if (magnitude <= 10000) {
      return { label: 'Auto-approved (≤ $10,000)', icon: 'check_circle', cssClass: 'auth-auto' };
    }
    if (magnitude <= 100000) {
      return { label: 'Supervisor approval required', icon: 'warning', cssClass: 'auth-warn' };
    }
    return { label: 'Manager approval required', icon: 'warning', cssClass: 'auth-warn' };
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const request: CreateReserveRequest = { ...value, amount: value.amount!, transactionType: 'Add' };
    this.dialogRef.close(request);
  }
}
