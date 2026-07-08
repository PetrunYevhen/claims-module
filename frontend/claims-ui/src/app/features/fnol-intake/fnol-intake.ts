import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import {
  AssetType,
  CreateClaimRequest,
  CreatePartyRequest,
  CreateRiskObjectRequest,
  PartyRole,
  PartyType
} from '../../core/models/claim.model';
import { CauseOfLossCode, Policy } from '../../core/models/reference.model';
import { ReserveComponentType } from '../../core/models/reserve.model';
import { ClaimsService } from '../../core/services/claims.service';
import { ReferenceDataService } from '../../core/services/reference-data.service';
import { ConfirmDialog } from '../../shared/confirm-dialog/confirm-dialog';

const PARTY_ROLES: PartyRole[] = ['Claimant', 'Insured', 'ThirdParty', 'Witness', 'Attorney'];
const ASSET_TYPES: AssetType[] = ['Vehicle', 'Property', 'Person', 'Equipment', 'Other'];
const RESERVE_COMPONENTS: ReserveComponentType[] = ['Indemnity', 'Expense', 'ALAE', 'SubrogationRecoverable'];

@Component({
  selector: 'app-fnol-intake',
  imports: [
    ReactiveFormsModule,
    MatStepperModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatAutocompleteModule,
    MatProgressSpinnerModule,
    DatePipe
  ],
  templateUrl: './fnol-intake.html',
  styleUrl: './fnol-intake.scss'
})
export class FnolIntake implements OnInit {
  private readonly claimsService = inject(ClaimsService);
  private readonly referenceDataService = inject(ReferenceDataService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly partyRoles = PARTY_ROLES;
  protected readonly assetTypes = ASSET_TYPES;
  protected readonly reserveComponents = RESERVE_COMPONENTS;

  protected readonly causeOfLossCodes = signal<CauseOfLossCode[]>([]);
  protected readonly policyResults = signal<Policy[]>([]);
  protected readonly selectedPolicy = signal<Policy | null>(null);
  protected readonly submitting = signal(false);

  protected readonly policySearchControl = new FormControl('', { nonNullable: true });

  protected readonly step1Form = new FormGroup({
    unknownPolicy: new FormControl(false, { nonNullable: true }),
    lossDate: new FormControl<Date | null>(null, [Validators.required]),
    causeOfLossCode: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    lossDescription: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(20)]
    }),
    lossLocation: new FormControl('', { nonNullable: true }),
    estimatedLossAmount: new FormControl<number | null>(null)
  });

  protected readonly parties = signal<CreatePartyRequest[]>([]);
  protected readonly riskObjects = signal<CreateRiskObjectRequest[]>([]);
  protected readonly showAddParty = signal(false);
  protected readonly showAddRiskObject = signal(false);

  protected readonly newPartyForm = new FormGroup({
    partyRole: new FormControl<PartyRole>('Claimant', { nonNullable: true, validators: [Validators.required] }),
    partyType: new FormControl<PartyType>('Person', { nonNullable: true, validators: [Validators.required] }),
    firstName: new FormControl('', { nonNullable: true }),
    lastName: new FormControl('', { nonNullable: true }),
    companyName: new FormControl('', { nonNullable: true }),
    email: new FormControl('', { nonNullable: true }),
    phone: new FormControl('', { nonNullable: true })
  });

  protected readonly newRiskObjectForm = new FormGroup({
    assetType: new FormControl<AssetType>('Vehicle', { nonNullable: true, validators: [Validators.required] }),
    assetDescription: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    damageDescription: new FormControl('', { nonNullable: true }),
    assetReference: new FormControl('', { nonNullable: true }),
    isPrimary: new FormControl(false, { nonNullable: true })
  });

  protected readonly step3Form = new FormGroup({
    reserveComponent: new FormControl<ReserveComponentType | null>(null),
    reserveAmount: new FormControl<number | null>(null),
    reserveChangeReason: new FormControl('', { nonNullable: true })
  });

  ngOnInit(): void {
    this.referenceDataService.getCauseOfLossCodes().subscribe((codes) => this.causeOfLossCodes.set(codes));

    this.policySearchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((query) =>
          typeof query === 'string' && query.trim().length >= 2 ? this.referenceDataService.searchPolicies(query) : of([])
        )
      )
      .subscribe((results) => this.policyResults.set(results));
  }

  selectPolicy(policy: Policy): void {
    this.selectedPolicy.set(policy);
    this.policySearchControl.setValue(`${policy.policyNumber} - ${policy.clientName}`, { emitEvent: false });
    this.policyResults.set([]);
    this.step1Form.controls.unknownPolicy.setValue(false);
  }

  clearPolicy(): void {
    this.selectedPolicy.set(null);
    this.policySearchControl.setValue('', { emitEvent: false });
  }

  /** Client-side mirror of BR-C-02, used for the in-force badge and pre-submit warning check. */
  protected inForceStatus(): 'in-force' | 'out-of-force' | null {
    const policy = this.selectedPolicy();
    const lossDate = this.step1Form.controls.lossDate.value;
    if (!policy || !lossDate) return null;

    const loss = new Date(lossDate).getTime();
    const effective = new Date(policy.effectiveDate).getTime();
    const expiration = new Date(policy.expirationDate).getTime();
    return loss >= effective && loss <= expiration ? 'in-force' : 'out-of-force';
  }

  toggleAddParty(): void {
    this.showAddParty.update((v) => !v);
  }

  addParty(): void {
    if (this.newPartyForm.invalid) {
      this.newPartyForm.markAllAsTouched();
      return;
    }
    this.parties.update((list) => [...list, this.newPartyForm.getRawValue()]);
    this.newPartyForm.reset({ partyRole: 'Claimant', partyType: 'Person' });
    this.showAddParty.set(false);
  }

  removeParty(index: number): void {
    this.parties.update((list) => list.filter((_, i) => i !== index));
  }

  toggleAddRiskObject(): void {
    this.showAddRiskObject.update((v) => !v);
  }

  addRiskObject(): void {
    if (this.newRiskObjectForm.invalid) {
      this.newRiskObjectForm.markAllAsTouched();
      return;
    }
    this.riskObjects.update((list) => [...list, this.newRiskObjectForm.getRawValue()]);
    this.newRiskObjectForm.reset({ assetType: 'Vehicle', isPrimary: false });
    this.showAddRiskObject.set(false);
  }

  removeRiskObject(index: number): void {
    this.riskObjects.update((list) => list.filter((_, i) => i !== index));
  }

  hasClaimant(): boolean {
    return this.parties().some((p) => p.partyRole === 'Claimant');
  }

  step2Valid(): boolean {
    return this.hasClaimant();
  }

  /** Real-time authority threshold preview - mirrors ReserveAuthorityRules on the backend. */
  authorityIndicator(): { label: string; icon: string; cssClass: string } | null {
    const amount = this.step3Form.controls.reserveAmount.value;
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

  canSubmit(): boolean {
    return this.step1Form.valid && this.step2Valid() && !this.submitting();
  }

  submit(): void {
    if (!this.canSubmit()) {
      this.step1Form.markAllAsTouched();
      return;
    }

    const hasWarnings = this.step1Form.controls.unknownPolicy.value || this.inForceStatus() === 'out-of-force';
    if (hasWarnings) {
      this.dialog
        .open(ConfirmDialog, {
          data: {
            title: 'Warnings on this claim',
            message: 'This claim has warnings and will be created in Draft status:',
            items: [
              ...(this.step1Form.controls.unknownPolicy.value
                ? ['No policy linked - financial actions will be blocked until one is associated.']
                : []),
              ...(this.inForceStatus() === 'out-of-force'
                ? ['Loss date falls outside the policy effective period.']
                : [])
            ],
            confirmLabel: 'Create anyway'
          }
        })
        .afterClosed()
        .subscribe((confirmed) => {
          if (confirmed) this.doSubmit();
        });
      return;
    }

    this.doSubmit();
  }

  private doSubmit(): void {
    this.submitting.set(true);
    const s1 = this.step1Form.getRawValue();
    const s3 = this.step3Form.getRawValue();

    const request: CreateClaimRequest = {
      policyId: s1.unknownPolicy ? null : (this.selectedPolicy()?.policyId ?? null),
      lossDate: s1.lossDate!.toISOString(),
      lossDescription: s1.lossDescription,
      lossLocation: s1.lossLocation || null,
      causeOfLossCode: s1.causeOfLossCode,
      estimatedLossAmount: s1.estimatedLossAmount,
      parties: this.parties(),
      riskObjects: this.riskObjects(),
      // FRS 5.2 Step 3 / 10.1: the optional initial reserve is created in the same call, so the
      // claim and its reserve commit as one transaction. BR-C-06: only send it when a policy is
      // linked (the Step 3 reserve fields are hidden otherwise).
      initialReserve:
        s3.reserveAmount && s3.reserveComponent && this.selectedPolicy()
          ? {
              component: s3.reserveComponent,
              amount: s3.reserveAmount,
              changeReason: s3.reserveChangeReason || 'Initial reserve at FNOL intake'
            }
          : null
    };

    this.claimsService.create(request).subscribe({
      next: (claim) => {
        this.snackBar.open(`Claim ${claim.claimNumber} created.`, 'View', { duration: 6000 });
        this.router.navigate(['/claims', claim.claimId]);
      },
      error: () => this.submitting.set(false)
    });
  }

  displayParty(party: CreatePartyRequest): string {
    const name = party.partyType === 'Company' ? party.companyName : `${party.firstName} ${party.lastName}`;
    return `${party.partyRole}: ${name || '(unnamed)'}`;
  }
}
