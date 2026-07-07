import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { CreatePartyRequest, PartyRole, PartyType } from '../../../../core/models/claim.model';

const PARTY_ROLES: PartyRole[] = ['Claimant', 'Insured', 'ThirdParty', 'Witness', 'Attorney'];

@Component({
  selector: 'app-add-party-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatSelectModule, MatInputModule, MatButtonModule],
  templateUrl: './add-party-dialog.html',
  styles: [
    `
      .dialog-form {
        display: flex;
        flex-direction: column;
        gap: 4px;
        min-width: 360px;
      }
      .field-row {
        display: flex;
        gap: 12px;

        mat-form-field {
          flex: 1 1 0;
        }
      }
      .full-width {
        width: 100%;
      }
    `
  ]
})
export class AddPartyDialog {
  private readonly dialogRef = inject(MatDialogRef<AddPartyDialog>);
  protected readonly partyRoles = PARTY_ROLES;

  protected readonly form = new FormGroup({
    partyRole: new FormControl<PartyRole>('Claimant', { nonNullable: true, validators: [Validators.required] }),
    partyType: new FormControl<PartyType>('Person', { nonNullable: true, validators: [Validators.required] }),
    firstName: new FormControl('', { nonNullable: true }),
    lastName: new FormControl('', { nonNullable: true }),
    companyName: new FormControl('', { nonNullable: true }),
    email: new FormControl('', { nonNullable: true }),
    phone: new FormControl('', { nonNullable: true })
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.dialogRef.close(this.form.getRawValue() as CreatePartyRequest);
  }
}
