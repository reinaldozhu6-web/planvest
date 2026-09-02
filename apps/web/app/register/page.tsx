"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { ArrowRight, UserRoundPlus } from "lucide-react";
import { useRouter } from "next/navigation";
import { apiFetch, saveSession } from "../lib/api";
import type { AuthResponse } from "../lib/types";

export default function RegisterPage() {
  const router = useRouter(); const [error, setError] = useState(""); const [loading, setLoading] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setLoading(true); setError(""); const data = new FormData(event.currentTarget);
    try {
      const result = await apiFetch<AuthResponse>("/api/auth/register", { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify({displayName:data.get("name"), email:data.get("email"), password:data.get("password")}) });
      saveSession(result); router.push("/dashboard");
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Could not create your account."); } finally { setLoading(false); }
  }
  return <main className="auth-page"><section className="auth-aside"><Link className="brand" href="/"><span>PV</span>PlanVest</Link><div><p className="eyebrow">Start with clarity</p><h1>A practical home for your investing plan.</h1><p>Create a simulated portfolio, complete an explainable risk assessment, and model your next financial goal.</p></div><small>No bank details required · Portfolio project</small></section><section className="auth-panel"><form className="auth-form" onSubmit={submit}><div className="auth-icon"><UserRoundPlus /></div><p className="eyebrow">Create your workspace</p><h2>Get started</h2><p className="form-intro">Use synthetic values if you prefer not to enter personal data.</p><label>Display name<input name="name" required minLength={2} maxLength={80} placeholder="Alex Chen" /></label><label>Email address<input name="email" type="email" autoComplete="email" required placeholder="alex@example.com" /></label><label>Password<input name="password" type="password" autoComplete="new-password" required minLength={10} placeholder="At least 10 characters" /></label>{error && <p className="form-error" role="alert">{error}</p>}<button className="button form-button" disabled={loading}>{loading ? "Creating…" : <>Create account <ArrowRight size={17} /></>}</button><p className="switch-link">Already registered? <Link href="/login">Sign in</Link></p></form></section></main>;
}
