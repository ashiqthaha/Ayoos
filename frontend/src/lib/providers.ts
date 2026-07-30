import type { ProviderField } from "@/components/provider-form-fields";
import type { ProviderInput } from "@/lib/api";

export const emptyProvider: ProviderInput = {
  firstName: "",
  lastName: "",
  credentials: "",
  specialty: "",
  email: "",
  phone: "",
};

export function validateProvider(
  provider: ProviderInput,
): Partial<Record<ProviderField, string>> {
  const errors: Partial<Record<ProviderField, string>> = {};

  if (!provider.firstName.trim()) errors.firstName = "First name is required.";
  if (!provider.lastName.trim()) errors.lastName = "Last name is required.";
  if (!provider.credentials.trim()) errors.credentials = "Credentials are required.";
  if (!provider.specialty.trim()) errors.specialty = "Specialty is required.";
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(provider.email.trim())) {
    errors.email = "Enter a valid email address.";
  }
  if (!provider.phone.trim()) errors.phone = "Phone is required.";

  return errors;
}

export function normalizeProvider(provider: ProviderInput): ProviderInput {
  return Object.fromEntries(
    Object.entries(provider).map(([key, value]) => [key, value.trim()]),
  ) as ProviderInput;
}

export function localIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function todayIsoDate(): string {
  return localIsoDate(new Date());
}

export function addDaysIso(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return localIsoDate(date);
}

export function shortTime(value: string): string {
  return value.slice(0, 5);
}

export function apiTime(value: string): string {
  return value.length === 5 ? `${value}:00` : value;
}

export function displayTime(value: string): string {
  const [hours, minutes] = shortTime(value).split(":").map(Number);
  return new Intl.DateTimeFormat(undefined, {
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(2000, 0, 1, hours, minutes));
}
