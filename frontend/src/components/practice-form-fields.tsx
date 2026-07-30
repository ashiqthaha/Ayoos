"use client";

import { useMemo } from "react";

import type { PracticeInput } from "@/lib/api";
import {
  getTimeZoneGroups,
  type PracticeField,
  type PracticeFieldErrors,
} from "@/lib/practice";

type FieldChange = (field: PracticeField, value: string) => void;

type FieldsProps = {
  value: PracticeInput;
  errors: PracticeFieldErrors;
  onChange: FieldChange;
};

const inputClass =
  "mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-[15px] text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10 disabled:cursor-not-allowed disabled:bg-slate-50";

type TextFieldProps = {
  id: PracticeField;
  label: string;
  value: string;
  error?: string;
  onChange: FieldChange;
  type?: "text" | "email" | "tel";
  placeholder?: string;
  autoComplete?: string;
  maxLength?: number;
  required?: boolean;
  helper?: string;
};

function TextField({
  id,
  label,
  value,
  error,
  onChange,
  type = "text",
  placeholder,
  autoComplete,
  maxLength,
  required = true,
  helper,
}: TextFieldProps) {
  const errorId = `${id.replace(".", "-")}-error`;

  return (
    <label className="block text-sm font-medium text-slate-700" htmlFor={id}>
      {label}
      {!required && <span className="ml-1 font-normal text-slate-400">(optional)</span>}
      <input
        id={id}
        name={id}
        type={type}
        value={value}
        onChange={(event) => onChange(id, event.target.value)}
        placeholder={placeholder}
        autoComplete={autoComplete}
        maxLength={maxLength}
        required={required}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : undefined}
        className={`${inputClass} ${error ? "border-rose-400 focus:border-rose-500 focus:ring-rose-500/10" : ""}`}
      />
      {error ? (
        <span id={errorId} className="mt-1.5 block text-sm font-normal text-rose-600">
          {error}
        </span>
      ) : helper ? (
        <span className="mt-1.5 block text-xs font-normal leading-5 text-slate-500">
          {helper}
        </span>
      ) : null}
    </label>
  );
}

export function PracticeIdentityFields({ value, errors, onChange }: FieldsProps) {
  const timeZoneGroups = useMemo(() => getTimeZoneGroups(), []);
  const timeZoneErrorId = "time-zone-error";

  return (
    <div className="grid gap-5">
      <TextField
        id="name"
        label="Practice name"
        value={value.name}
        error={errors.name}
        onChange={onChange}
        placeholder="Harbor Family Health"
        autoComplete="organization"
        maxLength={120}
      />
      <TextField
        id="slug"
        label="Practice URL slug"
        value={value.slug}
        error={errors.slug}
        onChange={onChange}
        placeholder="harbor-family-health"
        maxLength={120}
        helper="Used in your practice URL. Lowercase letters, numbers, and hyphens only."
      />
      <label className="block text-sm font-medium text-slate-700" htmlFor="timeZone">
        Time zone
        <select
          id="timeZone"
          name="timeZone"
          value={value.timeZone}
          onChange={(event) => onChange("timeZone", event.target.value)}
          required
          aria-invalid={Boolean(errors.timeZone)}
          aria-describedby={errors.timeZone ? timeZoneErrorId : undefined}
          className={`${inputClass} appearance-none ${errors.timeZone ? "border-rose-400 focus:border-rose-500 focus:ring-rose-500/10" : ""}`}
        >
          <option value="">Select a time zone</option>
          {timeZoneGroups.map((group) => (
            <optgroup key={group.region} label={group.region}>
              {group.zones.map((zone) => (
                <option key={zone} value={zone}>
                  {zone.replaceAll("_", " ")}
                </option>
              ))}
            </optgroup>
          ))}
        </select>
        {errors.timeZone && (
          <span id={timeZoneErrorId} className="mt-1.5 block text-sm font-normal text-rose-600">
            {errors.timeZone}
          </span>
        )}
      </label>
    </div>
  );
}

export function PracticeAddressFields({ value, errors, onChange }: FieldsProps) {
  return (
    <div className="grid gap-5">
      <TextField
        id="address.line1"
        label="Street address"
        value={value.address.line1}
        error={errors["address.line1"]}
        onChange={onChange}
        placeholder="125 Harbor Avenue"
        autoComplete="address-line1"
        maxLength={200}
      />
      <TextField
        id="address.line2"
        label="Suite, floor, or unit"
        value={value.address.line2}
        error={errors["address.line2"]}
        onChange={onChange}
        placeholder="Suite 400"
        autoComplete="address-line2"
        maxLength={200}
        required={false}
      />
      <div className="grid gap-5 sm:grid-cols-2">
        <TextField
          id="address.city"
          label="City"
          value={value.address.city}
          error={errors["address.city"]}
          onChange={onChange}
          placeholder="Portland"
          autoComplete="address-level2"
          maxLength={100}
        />
        <TextField
          id="address.state"
          label="State or region"
          value={value.address.state}
          error={errors["address.state"]}
          onChange={onChange}
          placeholder="Maine"
          autoComplete="address-level1"
          maxLength={100}
        />
      </div>
      <div className="grid gap-5 sm:grid-cols-2">
        <TextField
          id="address.postalCode"
          label="Postal code"
          value={value.address.postalCode}
          error={errors["address.postalCode"]}
          onChange={onChange}
          placeholder="04101"
          autoComplete="postal-code"
          maxLength={20}
        />
        <TextField
          id="address.country"
          label="Country"
          value={value.address.country}
          error={errors["address.country"]}
          onChange={onChange}
          placeholder="United States"
          autoComplete="country-name"
          maxLength={100}
        />
      </div>
    </div>
  );
}

export function PracticeContactFields({ value, errors, onChange }: FieldsProps) {
  return (
    <div className="grid gap-5 sm:grid-cols-2">
      <TextField
        id="contactEmail"
        label="Contact email"
        value={value.contactEmail}
        error={errors.contactEmail}
        onChange={onChange}
        type="email"
        placeholder="hello@harborhealth.com"
        autoComplete="email"
        maxLength={320}
      />
      <TextField
        id="contactPhone"
        label="Contact phone"
        value={value.contactPhone}
        error={errors.contactPhone}
        onChange={onChange}
        type="tel"
        placeholder="(207) 555-0142"
        autoComplete="tel"
        maxLength={50}
      />
    </div>
  );
}
