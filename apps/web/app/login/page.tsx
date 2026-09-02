"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { ArrowRight, Eye, LockKeyhole } from "lucide-react";
import { useRouter } from "next/navigation";

export default function LoginPage() {
  const router = useRouter();
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setLoading(true); setError("");
    const data = new FormData(event.currentTarget);
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080"}/api/auth/login`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: data.get("email"), password: data.get("password") }),
      });
      if (!response.ok) throw new Error(response.status === 401 ? "Email or password is incorrect." : "Could not sign in. Please try again.");
      const result = await response.json();
      sessionStorage.setItem("planvest_access_token", result.accessToken);
      router.push("/dashboard");
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Could not sign in."); }
    finally { setLoading(false); }
  }

  return <main className="auth-page"><section className="auth-aside"><Link className="brand" href="/"><span>PV</span>PlanVest</Link><div><p className="eyebrow">Your plan, made visible</p><h1>Good decisions start with a clear picture.</h1><p>Review what you own, understand your allocation, and see whether your contributions support your goals.</p></div><small>Educational planning only · All demo data is synthetic</small></section><section className="auth-panel"><form className="auth-form" onSubmit={submit}><div className="auth-icon"><LockKeyhole /></div><p className="eyebrow">Welcome back</p><h2>Sign in to PlanVest</h2><p className="form-intro">Continue to your private planning workspace.</p><label>Email address<input name="email" type="email" autoComplete="email" required placeholder="alex@example.com" /></label><label>Password<div className="password-field"><input name="password" type="password" autoComplete="current-password" required placeholder="Enter your password" /><Eye size={17} /></div></label>{error && <p className="form-error" role="alert">{error}</p>}<button className="button form-button" disabled={loading}>{loading ? "Signing in…" : <>Sign in <ArrowRight size={17} /></>}</button><div className="divider"><span>or</span></div><Link className="demo-link" href="/dashboard">Explore the synthetic demo</Link><p className="switch-link">New to PlanVest? <Link href="/register">Create an account</Link></p></form></section></main>;
}
