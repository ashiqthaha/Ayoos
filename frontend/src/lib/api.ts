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

export type DayOfWeek = 0 | 1 | 2 | 3 | 4 | 5 | 6;

export type AvailabilityRuleInput = {
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
  effectiveFrom: string;
  effectiveTo: string | null;
};

export type AvailabilityRule = AvailabilityRuleInput & {
  id: string;
  providerId: string;
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

  try {
    response = await fetch(`${getApiUrl()}${path}`, {
      ...init,
      cache: "no-store",
      headers: {
        Accept: "application/json",
        ...(init.body ? { "Content-Type": "application/json" } : {}),
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

export function getAvailabilityRules(
  slug: string,
  providerId: string,
  signal?: AbortSignal,
): Promise<AvailabilityRule[]> {
  return request<AvailabilityRule[]>(
    `/api/providers/${encodeURIComponent(providerId)}/availability-rules`,
    {
      headers: tenantHeaders(slug),
      signal,
    },
  );
}

export function setAvailabilityRules(
  slug: string,
  providerId: string,
  rules: AvailabilityRuleInput[],
): Promise<AvailabilityRule[]> {
  return request<AvailabilityRule[]>(
    `/api/providers/${encodeURIComponent(providerId)}/availability-rules`,
    {
      method: "PUT",
      headers: tenantHeaders(slug),
      body: JSON.stringify({ rules }),
    },
  );
}

export function getAvailabilityExceptions(
  slug: string,
  providerId: string,
  signal?: AbortSignal,
): Promise<AvailabilityException[]> {
  return request<AvailabilityException[]>(
    `/api/providers/${encodeURIComponent(providerId)}/availability-exceptions`,
    {
      headers: tenantHeaders(slug),
      signal,
    },
  );
}

export function addAvailabilityException(
  slug: string,
  providerId: string,
  input: AvailabilityExceptionInput,
): Promise<AvailabilityException> {
  return request<AvailabilityException>(
    `/api/providers/${encodeURIComponent(providerId)}/availability-exceptions`,
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
  const query = new URLSearchParams({ exceptionId });
  return request<void>(
    `/api/providers/${encodeURIComponent(providerId)}/availability-exceptions?${query}`,
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
    `/api/providers/${encodeURIComponent(providerId)}/slots?${query}`,
    {
      headers: tenantHeaders(slug),
      signal,
    },
  );
}
