import {
  getAccessToken,
  redirectToLoginAfterUnauthorized,
} from "@/lib/auth-client";

const defaultApiUrl = "http://localhost:5000";

export type PracticeAddress = {
  line1: string;
  line2: string | null;
  city: string;
  state: string;
  postalCode: string;
  country: string;
};

export type PracticeAddressInput = Omit<PracticeAddress, "line2"> & {
  line2: string;
};

export type PracticeInput = {
  name: string;
  slug: string;
  timeZone: string;
  address: PracticeAddressInput;
  contactEmail: string;
  contactPhone: string;
};

export type Practice = Omit<PracticeInput, "address"> & {
  address: PracticeAddress;
  id: string;
  createdAtUtc: string;
  isActive: boolean;
};

export type ProviderInput = {
  firstName: string;
  lastName: string;
  credentials: string;
  specialty: string;
  email: string;
  phone: string;
};

export type Provider = ProviderInput & {
  id: string;
  practiceId: string;
  isActive: boolean;
  createdAtUtc: string;
};

export type PatientSex = 0 | 1 | 2 | 3;

export type PatientAddress = {
  line1: string;
  line2: string | null;
  city: string;
  state: string;
  postalCode: string;
  country: string;
};

export type PatientAddressInput = Omit<PatientAddress, "line2"> & {
  line2: string;
};

export type EmergencyContact = {
  id: string;
  name: string;
  relationship: string;
  phone: string;
};

export type EmergencyContactInput = Omit<EmergencyContact, "id">;

export type PatientInput = {
  firstName: string;
  lastName: string;
  preferredName: string;
  dateOfBirth: string;
  sex: PatientSex;
  email: string;
  phone: string;
  address: PatientAddressInput;
  preferredLanguage: string;
  emergencyContact: EmergencyContactInput | null;
};

export type Patient = Omit<
  PatientInput,
  "preferredName" | "address" | "preferredLanguage" | "emergencyContact"
> & {
  id: string;
  practiceId: string;
  keycloakUserId: string | null;
  preferredName: string | null;
  address: PatientAddress;
  preferredLanguage: string | null;
  emergencyContact: EmergencyContact | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

export type PatientDuplicateMatch = Pick<
  Patient,
  "id" | "firstName" | "lastName" | "dateOfBirth" | "email" | "phone"
>;

export type RegisterPatientResult = {
  patient: Patient | null;
  duplicateWarning: boolean;
  possibleMatches: PatientDuplicateMatch[];
};

export type PagedList<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type DayOfWeek = 0 | 1 | 2 | 3 | 4 | 5 | 6;

export type AvailabilityScheduleInput = {
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
};

export type AvailabilitySchedule = AvailabilityScheduleInput & {
  id: string;
  providerId: string;
  tenantId: string;
  isActive: boolean;
};

export type AvailabilityExceptionInput = {
  date: string;
  isUnavailable: boolean;
  overrideStartTime: string | null;
  overrideEndTime: string | null;
  reason: string | null;
};

export type AvailabilityException = AvailabilityExceptionInput & {
  id: string;
  providerId: string;
};

export type AvailabilitySlot = {
  date: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  availabilityScheduleId: string | null;
};

export type BookingStatus = 0 | 1 | 2 | 3 | 4;

export type BookingInput = {
  patientId: string;
  providerId: string;
  availabilityScheduleId: string | null;
  startTime: string;
  endTime: string;
  reason: string | null;
};

export type Booking = BookingInput & {
  id: string;
  tenantId: string;
  status: BookingStatus;
  createdAt: string;
};

export type ProviderAvailability = {
  providerId: string;
  schedules: AvailabilitySchedule[];
  exceptions: AvailabilityException[];
};

export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  readonly status: number;
  readonly problem?: ProblemDetails;

  constructor(status: number, message: string, problem?: ProblemDetails) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }
}

export function getApiUrl(): string {
  return (process.env.NEXT_PUBLIC_API_URL || defaultApiUrl).replace(/\/$/, "");
}

export function getHealthUrl(): string {
  return `${getApiUrl()}/health`;
}

async function readProblemDetails(response: Response): Promise<ProblemDetails | undefined> {
  const contentType = response.headers.get("content-type") ?? "";

  if (!contentType.includes("json")) {
    return undefined;
  }

  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return undefined;
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  let response: Response;
  const accessToken = await getAccessToken();

  try {
    response = await fetch(`${getApiUrl()}${path}`, {
      ...init,
      cache: "no-store",
      headers: {
        Accept: "application/json",
        ...(init.body ? { "Content-Type": "application/json" } : {}),
        ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
        ...init.headers,
      },
    });
  } catch {
    throw new ApiError(
      0,
      "We couldn’t reach the Ayoos API. Make sure it is running and try again.",
    );
  }

  if (!response.ok) {
    if (response.status === 401) {
      await redirectToLoginAfterUnauthorized();
    }

    const problem = await readProblemDetails(response);
    const message =
      problem?.detail ||
      problem?.title ||
      `The request failed with status ${response.status}.`;

    throw new ApiError(response.status, message, problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function createPractice(input: PracticeInput): Promise<Practice> {
  return request<Practice>("/api/practices", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function getPractice(slug: string, signal?: AbortSignal): Promise<Practice> {
  return request<Practice>(`/api/practices/${encodeURIComponent(slug)}`, {
    headers: { "X-Tenant": slug },
    signal,
  });
}

export function updatePractice(
  currentSlug: string,
  input: PracticeInput,
  isActive: boolean,
): Promise<Practice> {
  return request<Practice>(`/api/practices/${encodeURIComponent(currentSlug)}`, {
    method: "PUT",
    headers: { "X-Tenant": currentSlug },
    body: JSON.stringify({ ...input, isActive }),
  });
}

function tenantHeaders(slug: string): HeadersInit {
  return { "X-Tenant": slug };
}

export function listProviders(
  slug: string,
  signal?: AbortSignal,
): Promise<Provider[]> {
  return request<Provider[]>("/api/providers", {
    headers: tenantHeaders(slug),
    signal,
  });
}

export function createProvider(
  slug: string,
  input: ProviderInput,
): Promise<Provider> {
  return request<Provider>("/api/providers", {
    method: "POST",
    headers: tenantHeaders(slug),
    body: JSON.stringify(input),
  });
}

export function getProvider(
  slug: string,
  providerId: string,
  signal?: AbortSignal,
): Promise<Provider> {
  return request<Provider>(`/api/providers/${encodeURIComponent(providerId)}`, {
    headers: tenantHeaders(slug),
    signal,
  });
}

export function updateProvider(
  slug: string,
  providerId: string,
  input: ProviderInput,
): Promise<Provider> {
  return request<Provider>(`/api/providers/${encodeURIComponent(providerId)}`, {
    method: "PUT",
    headers: tenantHeaders(slug),
    body: JSON.stringify(input),
  });
}

export function deactivateProvider(
  slug: string,
  providerId: string,
): Promise<Provider> {
  return request<Provider>(
    `/api/providers/${encodeURIComponent(providerId)}/deactivate`,
    {
      method: "POST",
      headers: tenantHeaders(slug),
    },
  );
}

export function listPatients(
  slug: string,
  options: {
    search?: string;
    page?: number;
    pageSize?: number;
    signal?: AbortSignal;
  } = {},
): Promise<PagedList<Patient>> {
  const query = new URLSearchParams({
    page: String(options.page ?? 1),
    pageSize: String(options.pageSize ?? 20),
  });
  if (options.search?.trim()) query.set("search", options.search.trim());

  return request<PagedList<Patient>>(`/api/patients?${query}`, {
    headers: tenantHeaders(slug),
    signal: options.signal,
  });
}

export function registerPatient(
  slug: string,
  input: PatientInput,
  confirmDuplicate = false,
): Promise<RegisterPatientResult> {
  return request<RegisterPatientResult>("/api/patients", {
    method: "POST",
    headers: tenantHeaders(slug),
    body: JSON.stringify({ ...input, confirmDuplicate }),
  });
}

export function getPatient(
  slug: string,
  patientId: string,
  signal?: AbortSignal,
): Promise<Patient> {
  return request<Patient>(`/api/patients/${encodeURIComponent(patientId)}`, {
    headers: tenantHeaders(slug),
    signal,
  });
}

export function getMyPatientRecord(
  slug: string,
  signal?: AbortSignal,
): Promise<Patient> {
  return request<Patient>("/api/patients/me", {
    headers: tenantHeaders(slug),
    signal,
  });
}

export function updatePatient(
  slug: string,
  patientId: string,
  input: PatientInput,
): Promise<Patient> {
  return request<Patient>(`/api/patients/${encodeURIComponent(patientId)}`, {
    method: "PUT",
    headers: tenantHeaders(slug),
    body: JSON.stringify(input),
  });
}

export function deactivatePatient(
  slug: string,
  patientId: string,
): Promise<Patient> {
  return request<Patient>(
    `/api/patients/${encodeURIComponent(patientId)}/deactivate`,
    {
      method: "POST",
      headers: tenantHeaders(slug),
    },
  );
}

export function getProviderAvailability(
  slug: string,
  providerId: string,
  signal?: AbortSignal,
): Promise<ProviderAvailability> {
  return request<ProviderAvailability>(
    `/api/providers/${encodeURIComponent(providerId)}/availability`,
    {
      headers: tenantHeaders(slug),
      signal,
    },
  );
}

export function createAvailability(
  slug: string,
  providerId: string,
  input: AvailabilityScheduleInput,
): Promise<AvailabilitySchedule> {
  return request<AvailabilitySchedule>(
    `/api/providers/${encodeURIComponent(providerId)}/availability`,
    {
      method: "POST",
      headers: tenantHeaders(slug),
      body: JSON.stringify(input),
    },
  );
}

export function updateAvailability(
  slug: string,
  providerId: string,
  availabilityId: string,
  input: AvailabilityScheduleInput,
): Promise<AvailabilitySchedule> {
  return request<AvailabilitySchedule>(
    `/api/providers/${encodeURIComponent(providerId)}/availability/${encodeURIComponent(availabilityId)}`,
    {
      method: "PUT",
      headers: tenantHeaders(slug),
      body: JSON.stringify(input),
    },
  );
}

export function deactivateAvailability(
  slug: string,
  providerId: string,
  availabilityId: string,
): Promise<void> {
  return request<void>(
    `/api/providers/${encodeURIComponent(providerId)}/availability/${encodeURIComponent(availabilityId)}`,
    {
      method: "DELETE",
      headers: tenantHeaders(slug),
    },
  );
}

export function addAvailabilityException(
  slug: string,
  providerId: string,
  input: AvailabilityExceptionInput,
): Promise<AvailabilityException> {
  return request<AvailabilityException>(
    `/api/providers/${encodeURIComponent(providerId)}/availability/exceptions`,
    {
      method: "POST",
      headers: tenantHeaders(slug),
      body: JSON.stringify(input),
    },
  );
}

export function removeAvailabilityException(
  slug: string,
  providerId: string,
  exceptionId: string,
): Promise<void> {
  return request<void>(
    `/api/providers/${encodeURIComponent(providerId)}/availability/exceptions/${encodeURIComponent(exceptionId)}`,
    {
      method: "DELETE",
      headers: tenantHeaders(slug),
    },
  );
}

export function getProviderSlots(
  slug: string,
  providerId: string,
  fromDate: string,
  toDate: string,
  signal?: AbortSignal,
): Promise<AvailabilitySlot[]> {
  const query = new URLSearchParams({ from: fromDate, to: toDate });
  return request<AvailabilitySlot[]>(
    `/api/providers/${encodeURIComponent(providerId)}/availability/slots?${query}`,
    {
      headers: tenantHeaders(slug),
      signal,
    },
  );
}

export function createBooking(
  slug: string,
  input: BookingInput,
): Promise<Booking> {
  return request<Booking>("/api/bookings", {
    method: "POST",
    headers: tenantHeaders(slug),
    body: JSON.stringify(input),
  });
}

export function getBooking(
  slug: string,
  bookingId: string,
  signal?: AbortSignal,
): Promise<Booking> {
  return request<Booking>(`/api/bookings/${encodeURIComponent(bookingId)}`, {
    headers: tenantHeaders(slug),
    signal,
  });
}

export function listBookings(
  slug: string,
  options: {
    providerId?: string;
    patientId?: string;
    fromDate?: string;
    toDate?: string;
    status?: BookingStatus;
    page?: number;
    pageSize?: number;
    signal?: AbortSignal;
  } = {},
): Promise<PagedList<Booking>> {
  const query = new URLSearchParams({
    page: String(options.page ?? 1),
    pageSize: String(options.pageSize ?? 20),
  });
  if (options.providerId) query.set("providerId", options.providerId);
  if (options.patientId) query.set("patientId", options.patientId);
  if (options.fromDate) query.set("fromDate", options.fromDate);
  if (options.toDate) query.set("toDate", options.toDate);
  if (options.status !== undefined) query.set("status", String(options.status));

  return request<PagedList<Booking>>(`/api/bookings?${query}`, {
    headers: tenantHeaders(slug),
    signal: options.signal,
  });
}

export function getProviderBookingSchedule(
  slug: string,
  providerId: string,
  fromDate: string,
  toDate: string,
  signal?: AbortSignal,
): Promise<Booking[]> {
  const query = new URLSearchParams({
    providerId,
    from: fromDate,
    to: toDate,
  });
  return request<Booking[]>(`/api/bookings/provider-schedule?${query}`, {
    headers: tenantHeaders(slug),
    signal,
  });
}

function transitionBooking(
  slug: string,
  bookingId: string,
  transition: "confirm" | "cancel" | "complete" | "no-show",
): Promise<Booking> {
  return request<Booking>(
    `/api/bookings/${encodeURIComponent(bookingId)}/${transition}`,
    {
      method: "POST",
      headers: tenantHeaders(slug),
    },
  );
}

export function confirmBooking(slug: string, bookingId: string): Promise<Booking> {
  return transitionBooking(slug, bookingId, "confirm");
}

export function cancelBooking(slug: string, bookingId: string): Promise<Booking> {
  return transitionBooking(slug, bookingId, "cancel");
}

export function completeBooking(slug: string, bookingId: string): Promise<Booking> {
  return transitionBooking(slug, bookingId, "complete");
}

export function markBookingNoShow(slug: string, bookingId: string): Promise<Booking> {
  return transitionBooking(slug, bookingId, "no-show");
}
