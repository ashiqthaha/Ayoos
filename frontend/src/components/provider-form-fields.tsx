import type { ProviderInput } from "@/lib/api";

export type ProviderField = keyof ProviderInput;

type Props = {
  value: ProviderInput;
  errors?: Partial<Record<ProviderField, string>>;
  disabled?: boolean;
  onChange: (field: ProviderField, value: string) => void;
};

const fields: Array<{
  key: ProviderField;
  label: string;
  type?: "text" | "email" | "tel";
  autoComplete?: string;
  placeholder: string;
}> = [
  {
    key: "firstName",
    label: "First name",
    autoComplete: "given-name",
    placeholder: "Maya",
  },
  {
    key: "lastName",
    label: "Last name",
    autoComplete: "family-name",
    placeholder: "Patel",
  },
  {
    key: "credentials",
    label: "Credentials",
    placeholder: "MD",
  },
  {
    key: "specialty",
    label: "Specialty",
    placeholder: "Family medicine",
  },
  {
    key: "email",
    label: "Email",
    type: "email",
    autoComplete: "email",
    placeholder: "maya@practice.com",
  },
  {
    key: "phone",
    label: "Phone",
    type: "tel",
    autoComplete: "tel",
    placeholder: "(212) 555-0124",
  },
];

export function ProviderFormFields({
  value,
  errors = {},
  disabled,
  onChange,
}: Props) {
  return (
    <div className="grid gap-5 sm:grid-cols-2">
      {fields.map((field) => (
        <label
          key={field.key}
          className="block text-sm font-medium text-slate-700"
          htmlFor={`provider-${field.key}`}
        >
          {field.label}
          <input
            id={`provider-${field.key}`}
            name={field.key}
            type={field.type ?? "text"}
            autoComplete={field.autoComplete}
            value={value[field.key]}
            disabled={disabled}
            onChange={(event) => onChange(field.key, event.target.value)}
            placeholder={field.placeholder}
            aria-invalid={Boolean(errors[field.key])}
            className={`mt-2 w-full rounded-xl border bg-white px-4 py-3 text-[15px] text-slate-900 outline-none transition placeholder:text-slate-400 focus:ring-4 disabled:cursor-not-allowed disabled:bg-slate-50 ${
              errors[field.key]
                ? "border-rose-400 focus:border-rose-500 focus:ring-rose-500/10"
                : "border-slate-200 focus:border-teal-500 focus:ring-teal-500/10"
            }`}
          />
          {errors[field.key] && (
            <span className="mt-1.5 block text-sm font-normal text-rose-600">
              {errors[field.key]}
            </span>
          )}
        </label>
      ))}
    </div>
  );
}
