import type { AuthResponse } from "./types";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";
const TOKEN_KEY = "planvest_access_token";

export class ApiError extends Error {
  constructor(message: string, public status: number) { super(message); }
}

export function hasSession() {
  return typeof window !== "undefined" && Boolean(sessionStorage.getItem(TOKEN_KEY));
}

export function saveSession(auth: AuthResponse) {
  sessionStorage.setItem(TOKEN_KEY, auth.accessToken);
}

export function clearSession() {
  if (typeof window !== "undefined") sessionStorage.removeItem(TOKEN_KEY);
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = typeof window === "undefined" ? null : sessionStorage.getItem(TOKEN_KEY);
  const headers = new Headers(init.headers);
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  if (token) headers.set("Authorization", `Bearer ${token}`);

  let response: Response;
  try {
    response = await fetch(`${API_URL}${path}`, { ...init, headers });
  } catch {
    throw new ApiError("The PlanVest API is unavailable. Start the API and try again.", 0);
  }

  if (!response.ok) {
    if (response.status === 401) clearSession();
    const problem = await response.json().catch(() => ({}));
    const validation = problem.errors
      ? Object.values(problem.errors as Record<string, string[]>).flat().join(" ")
      : "";
    throw new ApiError(validation || problem.detail || problem.title || "The request could not be completed.", response.status);
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export async function createDemoSession() {
  const auth = await apiFetch<AuthResponse>("/api/auth/demo-session", { method: "POST" });
  saveSession(auth);
  return auth;
}

export async function logout() {
  try { await apiFetch<void>("/api/auth/logout", { method: "POST" }); }
  finally { clearSession(); }
}

export function currency(value: number) {
  return new Intl.NumberFormat("en-CA", { style: "currency", currency: "CAD" }).format(value);
}

export function label(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2").replace("Tfsa", "TFSA").replace("Rrsp", "RRSP").replace("Fhsa", "FHSA");
}
