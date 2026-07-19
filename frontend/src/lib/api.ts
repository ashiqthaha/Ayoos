const defaultApiUrl = "http://localhost:5000";

export function getApiUrl(): string {
  return (process.env.NEXT_PUBLIC_API_URL || defaultApiUrl).replace(/\/$/, "");
}

export function getHealthUrl(): string {
  return `${getApiUrl()}/health`;
}
