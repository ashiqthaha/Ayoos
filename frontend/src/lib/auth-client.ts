import {
  InMemoryWebStorage,
  UserManager,
  WebStorageStateStore,
  type User,
} from "oidc-client-ts";

const defaultAuthority = "http://localhost:8081/realms/ayoos";
const defaultClientId = "ayoos-frontend";

let userManager: UserManager | undefined;
let renewal: Promise<User | null> | undefined;

function browserOrigin(): string {
  return window.location.origin;
}

export function getUserManager(): UserManager {
  if (typeof window === "undefined") {
    throw new Error("The OIDC user manager is only available in the browser.");
  }

  if (!userManager) {
    const origin = browserOrigin();

    userManager = new UserManager({
      authority:
        process.env.NEXT_PUBLIC_KEYCLOAK_AUTHORITY || defaultAuthority,
      client_id:
        process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || defaultClientId,
      redirect_uri: `${origin}/auth/callback`,
      silent_redirect_uri: `${origin}/auth/silent-callback`,
      post_logout_redirect_uri: `${origin}/login`,
      response_type: "code",
      scope: "openid profile email",
      automaticSilentRenew: true,
      accessTokenExpiringNotificationTimeInSeconds: 60,
      loadUserInfo: false,
      monitorSession: false,
      userStore: new WebStorageStateStore({
        store: new InMemoryWebStorage(),
      }),
      stateStore: new WebStorageStateStore({
        store: window.sessionStorage,
      }),
    });
  }

  return userManager;
}

export async function getAccessToken(): Promise<string | undefined> {
  if (typeof window === "undefined") {
    return undefined;
  }

  const manager = getUserManager();
  let user = await manager.getUser();

  if (user?.expired) {
    renewal ??= manager
      .signinSilent()
      .catch(() => null)
      .finally(() => {
        renewal = undefined;
      });
    user = await renewal;
  }

  return user?.access_token;
}

export async function redirectToLoginAfterUnauthorized(): Promise<void> {
  if (typeof window === "undefined") {
    return;
  }

  await getUserManager().removeUser();
  const currentPath = `${window.location.pathname}${window.location.search}`;
  window.location.assign(
    `/login?returnUrl=${encodeURIComponent(currentPath)}`,
  );
}

export type AyoosIdentity = {
  name: string;
  role: string;
  roleLabel: string;
};

type AccessTokenClaims = {
  name?: string;
  preferred_username?: string;
  email?: string;
  practice?: string;
  tenant?: string;
  realm_access?: {
    roles?: string[];
  };
};

const rolePriority = [
  "ayoos-superadmin",
  "practice-admin",
  "provider",
  "staff",
  "patient",
] as const;

const roleLabels: Record<string, string> = {
  "ayoos-superadmin": "Ayoos super-admin",
  "practice-admin": "Practice admin",
  provider: "Provider",
  staff: "Staff",
  patient: "Patient",
};

function decodeAccessToken(accessToken: string): AccessTokenClaims {
  try {
    const encodedPayload = accessToken.split(".")[1];
    if (!encodedPayload) return {};

    const base64 = encodedPayload
      .replaceAll("-", "+")
      .replaceAll("_", "/")
      .padEnd(Math.ceil(encodedPayload.length / 4) * 4, "=");

    return JSON.parse(atob(base64)) as AccessTokenClaims;
  } catch {
    return {};
  }
}

export function getRealmRoles(user: User): string[] {
  return decodeAccessToken(user.access_token).realm_access?.roles ?? [];
}

export function getPracticeSlug(user: User): string | undefined {
  const claims = decodeAccessToken(user.access_token);
  const practiceSlug = claims.practice || claims.tenant;

  return typeof practiceSlug === "string" && practiceSlug.trim()
    ? practiceSlug.trim()
    : undefined;
}

export function getAyoosIdentity(user: User): AyoosIdentity {
  const claims = decodeAccessToken(user.access_token);
  const roles = getRealmRoles(user);
  const role = rolePriority.find((candidate) => roles.includes(candidate))
    ?? roles[0]
    ?? "authenticated";

  const name =
    (typeof user.profile.name === "string" && user.profile.name)
    || claims.name
    || (typeof user.profile.preferred_username === "string"
      && user.profile.preferred_username)
    || claims.preferred_username
    || (typeof user.profile.email === "string" && user.profile.email)
    || claims.email
    || "Signed-in user";

  return {
    name,
    role,
    roleLabel: roleLabels[role] ?? role,
  };
}
