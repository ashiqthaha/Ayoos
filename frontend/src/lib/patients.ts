import type { Patient, PatientInput } from "@/lib/api";

export type PatientField =
  | "firstName"
  | "lastName"
  | "preferredName"
  | "dateOfBirth"
  | "sex"
  | "email"
  | "phone"
  | "preferredLanguage"
  | "address.line1"
  | "address.line2"
  | "address.city"
  | "address.state"
  | "address.postalCode"
  | "address.country"
  | "emergencyContact.name"
  | "emergencyContact.relationship"
  | "emergencyContact.phone";

export type PatientFieldErrors = Partial<Record<PatientField | "contact", string>>;

export const emptyPatient: PatientInput = {
  firstName: "",
  lastName: "",
  preferredName: "",
  dateOfBirth: "",
  sex: 3,
  email: "",
  phone: "",
  address: {
    line1: "",
    line2: "",
    city: "",
    state: "",
    postalCode: "",
    country: "United States",
  },
  preferredLanguage: "",
  emergencyContact: null,
};

export const emptyEmergencyContact = {
  name: "",
  relationship: "",
  phone: "",
};

export function patientCopy(): PatientInput {
  return {
    ...emptyPatient,
    address: { ...emptyPatient.address },
  };
}

export function patientToInput(patient: Patient): PatientInput {
  return {
    firstName: patient.firstName,
    lastName: patient.lastName,
    preferredName: patient.preferredName ?? "",
    dateOfBirth: patient.dateOfBirth,
    sex: patient.sex,
    email: patient.email,
    phone: patient.phone,
    address: {
      ...patient.address,
      line2: patient.address.line2 ?? "",
    },
    preferredLanguage: patient.preferredLanguage ?? "",
    emergencyContact: patient.emergencyContact
      ? {
          name: patient.emergencyContact.name,
          relationship: patient.emergencyContact.relationship,
          phone: patient.emergencyContact.phone,
        }
      : null,
  };
}

export function normalizePatient(input: PatientInput): PatientInput {
  return {
    firstName: input.firstName.trim(),
    lastName: input.lastName.trim(),
    preferredName: input.preferredName.trim(),
    dateOfBirth: input.dateOfBirth,
    sex: input.sex,
    email: input.email.trim(),
    phone: input.phone.trim(),
    address: {
      line1: input.address.line1.trim(),
      line2: input.address.line2.trim(),
      city: input.address.city.trim(),
      state: input.address.state.trim(),
      postalCode: input.address.postalCode.trim(),
      country: input.address.country.trim(),
    },
    preferredLanguage: input.preferredLanguage.trim(),
    emergencyContact: input.emergencyContact
      ? {
          name: input.emergencyContact.name.trim(),
          relationship: input.emergencyContact.relationship.trim(),
          phone: input.emergencyContact.phone.trim(),
        }
      : null,
  };
}

export function updatePatientField(
  input: PatientInput,
  field: PatientField,
  value: string,
): PatientInput {
  if (field === "sex") {
    return { ...input, sex: Number(value) as PatientInput["sex"] };
  }
  if (field.startsWith("address.")) {
    const key = field.slice("address.".length) as keyof PatientInput["address"];
    return { ...input, address: { ...input.address, [key]: value } };
  }
  if (field.startsWith("emergencyContact.")) {
    if (!input.emergencyContact) return input;
    const key = field.slice("emergencyContact.".length) as keyof NonNullable<PatientInput["emergencyContact"]>;
    return {
      ...input,
      emergencyContact: { ...input.emergencyContact, [key]: value },
    };
  }
  return { ...input, [field]: value };
}

export function validatePatient(input: PatientInput): PatientFieldErrors {
  const errors: PatientFieldErrors = {};
  const email = input.email.trim();
  const phone = input.phone.trim();

  if (!input.firstName.trim()) errors.firstName = "First name is required.";
  if (!input.lastName.trim()) errors.lastName = "Last name is required.";
  if (input.preferredName.trim().length > 100) {
    errors.preferredName = "Preferred name must be 100 characters or fewer.";
  }

  if (!input.dateOfBirth) {
    errors.dateOfBirth = "Date of birth is required.";
  } else {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const date = new Date(`${input.dateOfBirth}T00:00:00`);
    const oldest = new Date(today);
    oldest.setFullYear(oldest.getFullYear() - 130);
    if (date >= today || date < oldest) {
      errors.dateOfBirth = "Enter a past date of birth within the last 130 years.";
    }
  }

  if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    errors.email = "Enter a valid email address.";
  }
  if (!email && !phone) {
    errors.contact = "Enter at least an email address or phone number.";
  }

  if (!input.address.line1.trim()) errors["address.line1"] = "Street address is required.";
  if (!input.address.city.trim()) errors["address.city"] = "City is required.";
  if (!input.address.state.trim()) errors["address.state"] = "State or region is required.";
  if (!input.address.postalCode.trim()) errors["address.postalCode"] = "Postal code is required.";
  if (!input.address.country.trim()) errors["address.country"] = "Country is required.";

  if (input.emergencyContact) {
    if (!input.emergencyContact.name.trim()) {
      errors["emergencyContact.name"] = "Contact name is required.";
    }
    if (!input.emergencyContact.relationship.trim()) {
      errors["emergencyContact.relationship"] = "Relationship is required.";
    }
    if (!input.emergencyContact.phone.trim()) {
      errors["emergencyContact.phone"] = "Contact phone is required.";
    }
  }

  return errors;
}

export function formatPatientName(patient: Pick<Patient, "firstName" | "lastName" | "preferredName">): string {
  const preferred = patient.preferredName?.trim();
  return preferred
    ? `${patient.firstName} “${preferred}” ${patient.lastName}`
    : `${patient.firstName} ${patient.lastName}`;
}

export function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    timeZone: "UTC",
  }).format(new Date(`${value}T00:00:00Z`));
}
