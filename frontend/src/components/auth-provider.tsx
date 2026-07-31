"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import type { User } from "oidc-client-ts";

import {
  getAyoosIdentity,
  getUserManager,
  type AyoosIdentity,
} from "@/lib/auth-client";

type AuthContextValue = {
  user: User | null;
  identity: AyoosIdentity | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  signIn: (returnUrl?: string) => Promise<void>;
  completeSignIn: () => Promise<User>;
  completeSilentSignIn: () => Promise<void>;
  signOut: () => Promise<void>;
  clearSession: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

const callbackPaths = new Set([
  "/auth/callback",
  "/auth/silent-callback",
]);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const manager = getUserManager();
    const handleUserLoaded = (loadedUser: User) => setUser(loadedUser);
    const handleUserUnloaded = () => setUser(null);
    let active = true;

    manager.events.addUserLoaded(handleUserLoaded);
    manager.events.addUserUnloaded(handleUserUnloaded);
    manager.events.addUserSignedOut(handleUserUnloaded);

    async function restoreSession() {
      if (callbackPaths.has(window.location.pathname)) {
        if (active) setIsLoading(false);
        return;
      }

      try {
        const storedUser = await manager.getUser();
        if (storedUser && !storedUser.expired) {
          if (active) setUser(storedUser);
          return;
        }

        const restoredUser = await manager.signinSilent();
        if (active) setUser(restoredUser);
      } catch {
        if (active) setUser(null);
      } finally {
        if (active) setIsLoading(false);
      }
    }

    void restoreSession();

    return () => {
      active = false;
      manager.events.removeUserLoaded(handleUserLoaded);
      manager.events.removeUserUnloaded(handleUserUnloaded);
      manager.events.removeUserSignedOut(handleUserUnloaded);
    };
  }, []);

  const signIn = useCallback(async (returnUrl = "/setup/practice") => {
    await getUserManager().signinRedirect({
      state: { returnUrl },
    });
  }, []);

  const completeSignIn = useCallback(async () => {
    const signedInUser = await getUserManager().signinRedirectCallback();
    setUser(signedInUser);
    return signedInUser;
  }, []);

  const completeSilentSignIn = useCallback(async () => {
    await getUserManager().signinSilentCallback();
  }, []);

  const signOut = useCallback(async () => {
    await getUserManager().signoutRedirect();
  }, []);

  const clearSession = useCallback(async () => {
    await getUserManager().removeUser();
    setUser(null);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      identity: user ? getAyoosIdentity(user) : null,
      isAuthenticated: Boolean(user && !user.expired),
      isLoading,
      signIn,
      completeSignIn,
      completeSilentSignIn,
      signOut,
      clearSession,
    }),
    [
      clearSession,
      completeSignIn,
      completeSilentSignIn,
      isLoading,
      signIn,
      signOut,
      user,
    ],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider.");
  }

  return context;
}
