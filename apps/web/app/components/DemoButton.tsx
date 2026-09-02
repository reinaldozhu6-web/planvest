"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowRight } from "lucide-react";
import { createDemoSession } from "../lib/api";

export default function DemoButton({ className = "button" }: { className?: string }) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function openDemo() {
    setLoading(true); setError("");
    try { await createDemoSession(); router.push("/dashboard"); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Could not start the demo."); setLoading(false); }
  }

  return <div className="demo-action"><button type="button" className={className} onClick={openDemo} disabled={loading}>{loading ? "Preparing demo…" : <>Explore demo <ArrowRight size={17} /></>}</button>{error && <small role="alert">{error}</small>}</div>;
}
