import type { PatientInput, PatientSex } from "@/lib/api";
import {
  emptyEmergencyContact,
  type PatientField,
  type PatientFieldErrors,
} from "@/lib/patients";

type Props = {
  value: PatientInput;
  errors?: PatientFieldErrors;
  disabled?: boolean;
  onChange: (field: PatientField, value: string) => void;
  onEmergencyContactChange: (enabled: boolean) => void;
};

const inputClass = (hasError: boolean) =>
  `mt-2 w-full rounded-xl border bg-white px-4 py-3 text-[15px] text-slate-900 outline-none transition placeholder:text-slate-400 focus:ring-4 disabled:cursor-not-allowed disabled:bg-slate-50 ${
    hasError
      ? "border-rose-400 focus:border-rose-500 focus:ring-rose-500/10"
      : "border-slate-200 focus:border-teal-500 focus:ring-teal-500/10"
  }`;

function FieldError({ message }: { message?: string }) {
  return message ? (
    <span className="mt-1.5 block text-sm font-normal text-rose-600">{message}</span>
  ) : null;
}

export function PatientFormFields({
  value,
  errors = {},
  disabled,
  onChange,
  onEmergencyContactChange,
}: Props) {
  return (
    <div className="grid gap-9">
      <section>
        <h2 className="text-sm font-semibold uppercase tracking-[0.14em] text-teal-700">
          Demographics
        </h2>
        <div className="mt-5 grid gap-5 sm:grid-cols-2">
          <TextField label="First name" field="firstName" value={value.firstName} error={errors.firstName} disabled={disabled} autoComplete="given-name" onChange={onChange} />
          <TextField label="Last name" field="lastName" value={value.lastName} error={errors.lastName} disabled={disabled} autoComplete="family-name" onChange={onChange} />
          <TextField label="Preferred name (optional)" field="preferredName" value={value.preferredName} error={errors.preferredName} disabled={disabled} onChange={onChange} />
          <label className="block text-sm font-medium text-slate-700">
            Date of birth
            <input type="date" value={value.dateOfBirth} disabled={disabled} onChange={(event) => onChange("dateOfBirth", event.target.value)} aria-invalid={Boolean(errors.dateOfBirth)} className={inputClass(Boolean(errors.dateOfBirth))} />
            <FieldError message={errors.dateOfBirth} />
          </label>
          <label className="block text-sm font-medium text-slate-700">
            Sex
            <select value={value.sex} disabled={disabled} onChange={(event) => onChange("sex", event.target.value)} className={inputClass(Boolean(errors.sex))}>
              {([[3, "Unknown"], [0, "Female"], [1, "Male"], [2, "Other"]] as Array<[PatientSex, string]>).map(([sex, label]) => (
                <option key={sex} value={sex}>{label}</option>
              ))}
            </select>
          </label>
          <TextField label="Preferred language (optional)" field="preferredLanguage" value={value.preferredLanguage} error={errors.preferredLanguage} disabled={disabled} onChange={onChange} />
        </div>
      </section>

      <section className="border-t border-slate-100 pt-9">
        <h2 className="text-sm font-semibold uppercase tracking-[0.14em] text-teal-700">Contact</h2>
        <p className="mt-2 text-sm text-slate-500">At least one email address or phone number is required.</p>
        {errors.contact && <p className="mt-3 rounded-xl bg-rose-50 px-4 py-3 text-sm text-rose-700">{errors.contact}</p>}
        <div className="mt-5 grid gap-5 sm:grid-cols-2">
          <TextField label="Email" field="email" value={value.email} error={errors.email} disabled={disabled} type="email" autoComplete="email" onChange={onChange} />
          <TextField label="Phone" field="phone" value={value.phone} error={errors.phone} disabled={disabled} type="tel" autoComplete="tel" onChange={onChange} />
        </div>
      </section>

      <section className="border-t border-slate-100 pt-9">
        <h2 className="text-sm font-semibold uppercase tracking-[0.14em] text-teal-700">Address</h2>
        <div className="mt-5 grid gap-5 sm:grid-cols-2">
          <TextField label="Street address" field="address.line1" value={value.address.line1} error={errors["address.line1"]} disabled={disabled} autoComplete="address-line1" onChange={onChange} />
          <TextField label="Address line 2 (optional)" field="address.line2" value={value.address.line2} error={errors["address.line2"]} disabled={disabled} autoComplete="address-line2" onChange={onChange} />
          <TextField label="City" field="address.city" value={value.address.city} error={errors["address.city"]} disabled={disabled} autoComplete="address-level2" onChange={onChange} />
          <TextField label="State or region" field="address.state" value={value.address.state} error={errors["address.state"]} disabled={disabled} autoComplete="address-level1" onChange={onChange} />
          <TextField label="Postal code" field="address.postalCode" value={value.address.postalCode} error={errors["address.postalCode"]} disabled={disabled} autoComplete="postal-code" onChange={onChange} />
          <TextField label="Country" field="address.country" value={value.address.country} error={errors["address.country"]} disabled={disabled} autoComplete="country-name" onChange={onChange} />
        </div>
      </section>

      <section className="border-t border-slate-100 pt-9">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-sm font-semibold uppercase tracking-[0.14em] text-teal-700">Emergency contact</h2>
            <p className="mt-2 text-sm text-slate-500">Optional, but recommended.</p>
          </div>
          <button type="button" disabled={disabled} onClick={() => onEmergencyContactChange(!value.emergencyContact)} className="rounded-xl border border-teal-200 px-4 py-2 text-sm font-semibold text-teal-700 transition hover:bg-teal-50 disabled:opacity-50">
            {value.emergencyContact ? "Remove contact" : "Add contact"}
          </button>
        </div>
        {value.emergencyContact && (
          <div className="mt-5 grid gap-5 sm:grid-cols-3">
            <TextField label="Name" field="emergencyContact.name" value={value.emergencyContact.name} error={errors["emergencyContact.name"]} disabled={disabled} onChange={onChange} />
            <TextField label="Relationship" field="emergencyContact.relationship" value={value.emergencyContact.relationship} error={errors["emergencyContact.relationship"]} disabled={disabled} onChange={onChange} />
            <TextField label="Phone" field="emergencyContact.phone" value={value.emergencyContact.phone} error={errors["emergencyContact.phone"]} disabled={disabled} type="tel" onChange={onChange} />
          </div>
        )}
      </section>
    </div>
  );
}

type TextFieldProps = {
  label: string;
  field: PatientField;
  value: string;
  error?: string;
  disabled?: boolean;
  type?: "text" | "email" | "tel";
  autoComplete?: string;
  onChange: (field: PatientField, value: string) => void;
};

function TextField({ label, field, value, error, disabled, type = "text", autoComplete, onChange }: TextFieldProps) {
  return (
    <label className="block text-sm font-medium text-slate-700">
      {label}
      <input type={type} value={value} disabled={disabled} autoComplete={autoComplete} onChange={(event) => onChange(field, event.target.value)} aria-invalid={Boolean(error)} className={inputClass(Boolean(error))} />
      <FieldError message={error} />
    </label>
  );
}

export function addEmptyEmergencyContact(input: PatientInput): PatientInput {
  return { ...input, emergencyContact: { ...emptyEmergencyContact } };
}
