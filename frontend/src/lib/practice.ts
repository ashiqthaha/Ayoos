import type { PracticeInput } from "@/lib/api";

export type PracticeField =
  | "name"
  | "slug"
  | "timeZone"
  | "address.line1"
  | "address.line2"
  | "address.city"
  | "address.state"
  | "address.postalCode"
  | "address.country"
  | "contactEmail"
  | "contactPhone";

export type PracticeFieldErrors = Partial<Record<PracticeField, string>>;

export const emptyPractice: PracticeInput = {
  name: "",
  slug: "",
  timeZone: "",
  address: {
    line1: "",
    line2: "",
    city: "",
    state: "",
    postalCode: "",
    country: "United States",
  },
  contactEmail: "",
  contactPhone: "",
};

export const stepFields: Record<number, PracticeField[]> = {
  1: ["name", "slug", "timeZone"],
  2: [
    "address.line1",
    "address.line2",
    "address.city",
    "address.state",
    "address.postalCode",
    "address.country",
  ],
  3: ["contactEmail", "contactPhone"],
};

export function toKebabCase(value: string): string {
  return value
    .normalize("NFKD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

export function normalizePractice(input: PracticeInput): PracticeInput {
  return {
    name: input.name.trim(),
    slug: input.slug.trim(),
    timeZone: input.timeZone.trim(),
    address: {
      line1: input.address.line1.trim(),
      line2: input.address.line2.trim(),
      city: input.address.city.trim(),
      state: input.address.state.trim(),
      postalCode: input.address.postalCode.trim(),
      country: input.address.country.trim(),
    },
    contactEmail: input.contactEmail.trim(),
    contactPhone: input.contactPhone.trim(),
  };
}

export function validatePractice(input: PracticeInput): PracticeFieldErrors {
  const errors: PracticeFieldErrors = {};
  const name = input.name.trim();
  const email = input.contactEmail.trim();

  if (name.length < 2 || name.length > 120) {
    errors.name = "Practice name must be between 2 and 120 characters.";
  }

  if (!input.slug) {
    errors.slug = "Choose a URL slug for your practice.";
  } else if (input.slug.length > 120) {
    errors.slug = "Slug must be 120 characters or fewer.";
  } else if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(input.slug)) {
    errors.slug = "Use lowercase letters, numbers, and single hyphens only.";
  }

  if (!input.timeZone) {
    errors.timeZone = "Select your practice time zone.";
  } else if (input.timeZone.length > 100) {
    errors.timeZone = "Time zone must be 100 characters or fewer.";
  }

  if (!input.address.line1.trim()) {
    errors["address.line1"] = "Street address is required.";
  } else if (input.address.line1.trim().length > 200) {
    errors["address.line1"] = "Street address must be 200 characters or fewer.";
  }

  if (input.address.line2.trim().length > 200) {
    errors["address.line2"] = "Address line 2 must be 200 characters or fewer.";
  }

  if (!input.address.city.trim()) {
    errors["address.city"] = "City is required.";
  } else if (input.address.city.trim().length > 100) {
    errors["address.city"] = "City must be 100 characters or fewer.";
  }

  if (!input.address.state.trim()) {
    errors["address.state"] = "State or region is required.";
  } else if (input.address.state.trim().length > 100) {
    errors["address.state"] = "State or region must be 100 characters or fewer.";
  }

  if (!input.address.postalCode.trim()) {
    errors["address.postalCode"] = "Postal code is required.";
  } else if (input.address.postalCode.trim().length > 20) {
    errors["address.postalCode"] = "Postal code must be 20 characters or fewer.";
  }

  if (!input.address.country.trim()) {
    errors["address.country"] = "Country is required.";
  } else if (input.address.country.trim().length > 100) {
    errors["address.country"] = "Country must be 100 characters or fewer.";
  }

  if (!email) {
    errors.contactEmail = "Contact email is required.";
  } else if (email.length > 320 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    errors.contactEmail = "Enter a valid email address.";
  }

  if (!input.contactPhone.trim()) {
    errors.contactPhone = "Contact phone is required.";
  } else if (input.contactPhone.trim().length > 50) {
    errors.contactPhone = "Phone number must be 50 characters or fewer.";
  }

  return errors;
}

const fallbackTimeZones = [
  "UTC",
  "Africa/Cairo",
  "Africa/Johannesburg",
  "Africa/Lagos",
  "America/Anchorage",
  "America/Chicago",
  "America/Denver",
  "America/Halifax",
  "America/Los_Angeles",
  "America/Mexico_City",
  "America/New_York",
  "America/Phoenix",
  "America/Sao_Paulo",
  "America/Toronto",
  "Asia/Dubai",
  "Asia/Hong_Kong",
  "Asia/Kolkata",
  "Asia/Seoul",
  "Asia/Shanghai",
  "Asia/Singapore",
  "Asia/Tokyo",
  "Australia/Adelaide",
  "Australia/Brisbane",
  "Australia/Melbourne",
  "Australia/Perth",
  "Australia/Sydney",
  "Europe/Amsterdam",
  "Europe/Berlin",
  "Europe/London",
  "Europe/Madrid",
  "Europe/Paris",
  "Europe/Rome",
  "Pacific/Auckland",
  "Pacific/Honolulu",
];

export type TimeZoneGroup = { region: string; zones: string[] };

export function getTimeZoneGroups(): TimeZoneGroup[] {
  let zones = fallbackTimeZones;

  if (typeof Intl.supportedValuesOf === "function") {
    zones = ["UTC", ...Intl.supportedValuesOf("timeZone")];
  }

  const groups = new Map<string, string[]>();

  for (const zone of zones) {
    const region = zone.includes("/") ? zone.split("/")[0] : "Universal";
    const entries = groups.get(region) ?? [];
    entries.push(zone);
    groups.set(region, entries);
  }

  return [...groups.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([region, entries]) => ({ region, zones: entries }));
}

export function getBrowserTimeZone(): string {
  return Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
}
