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
