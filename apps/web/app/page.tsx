import Link from "next/link";
import { ArrowRight, BarChart3, Check, ShieldCheck, Sparkles, Target } from "lucide-react";

export default function Home() {
  return <main className="landing">
    <nav className="public-nav">
      <Link className="brand" href="/"><span>PV</span>PlanVest</Link>
      <div><Link className="text-link" href="/login">Sign in</Link><Link className="button small" href="/register">Create account</Link></div>
    </nav>
    <section className="hero">
      <div className="hero-copy">
        <p className="eyebrow"><Sparkles size={14} /> Educational investing workspace</p>
        <h1>See your portfolio.<br />Plan with purpose.</h1>
        <p>Bring simulated accounts, risk tolerance, and financial goals into one calm, explainable dashboard.</p>
        <div className="hero-actions"><Link className="button" href="/dashboard">Explore demo <ArrowRight size={17} /></Link><Link className="outline-button" href="/register">Build my plan</Link></div>
        <p className="fine-print"><ShieldCheck size={14} /> No brokerage connection. No trades. No financial advice.</p>
      </div>
      <div className="hero-card" aria-label="Sample portfolio overview">
        <div className="mock-header"><span>Portfolio overview</span><b>Demo</b></div>
        <p className="mock-label">Total portfolio value</p><strong className="mock-value">$84,250.40</strong><span className="positive">+$6,921.18 · 8.95%</span>
        <div className="allocation-bars"><i style={{height:"46%"}} /><i style={{height:"67%"}} /><i style={{height:"54%"}} /><i style={{height:"82%"}} /><i style={{height:"74%"}} /><i style={{height:"94%"}} /><i style={{height:"86%"}} /><i style={{height:"100%"}} /></div>
        <div className="mock-grid"><div><BarChart3 size={16} /><span>Allocation</span><b>72% equity</b></div><div><Target size={16} /><span>Goal progress</span><b>64% funded</b></div></div>
      </div>
    </section>
    <section className="trust-row"><span><Check /> Explainable risk score</span><span><Check /> Decimal financial math</span><span><Check /> Synthetic demo data</span></section>
  </main>;
}
