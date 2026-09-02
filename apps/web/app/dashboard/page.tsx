"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Bell, CircleDollarSign, Gauge, LayoutDashboard, LogOut, Menu, ShieldCheck, Target, TrendingUp, WalletCards } from "lucide-react";
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";
import PortfolioSection from "../components/PortfolioSection";
import RiskSection from "../components/RiskSection";
import PlanningSection from "../components/PlanningSection";
import { ApiError, apiFetch, currency, hasSession, label, logout } from "../lib/api";
import type { DashboardData } from "../lib/types";

const colors = ["#74b8aa", "#4e8178", "#c5a36a", "#8d7ab8", "#66736f", "#b87474"];

function SideNav({ name, onLogout }: { name: string; onLogout: () => void }) {
  const initials = name.split(" ").map(value => value[0]).join("").slice(0, 2).toUpperCase();
  return <aside className="sidebar"><Link className="brand" href="/"><span>PV</span>PlanVest</Link><nav><a className="active" href="#overview"><LayoutDashboard />Overview</a><a href="#portfolio"><WalletCards />Accounts</a><a href="#risk"><Gauge />Risk profile</a><a href="#planning"><Target />Goals & plan</a><a href="#simulator"><TrendingUp />Simulator</a></nav><div className="sidebar-bottom"><button type="button" className="sidebar-link" onClick={onLogout}><LogOut />Sign out</button><div className="demo-person"><span>{initials}</span><div><b>{name}</b><small>Private workspace</small></div></div></div></aside>;
}

export default function Dashboard() {
  const router = useRouter();
  const [data, setData] = useState<DashboardData | null>(null);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    try { setData(await apiFetch<DashboardData>("/api/dashboard")); setError(""); }
    catch (reason) {
      if (reason instanceof ApiError && reason.status === 401) { router.replace("/login"); return; }
      setError(reason instanceof Error ? reason.message : "Could not load the dashboard.");
    }
  }, [router]);

  useEffect(() => {
    const startup = window.setTimeout(() => {
      if (!hasSession()) router.replace("/login"); else void load();
    }, 0);
    return () => window.clearTimeout(startup);
  }, [load, router]);

  async function signOut() { await logout(); router.replace("/"); }

  if (!data) return <main className="loading-page"><Link className="brand" href="/"><span>PV</span>PlanVest</Link><div className="loading-mark" /><p>{error || "Loading your planning workspace…"}</p>{error && <button className="outline-button" onClick={() => void load()}>Try again</button>}</main>;

  const allocation = data.portfolio.allocation.map((item, index) => ({ ...item, name: label(item.assetClass), color: colors[index % colors.length] }));
  const primaryGoal = data.goals.find(goal => goal.status === "Active");
  const firstName = data.user.displayName.split(" ")[0];

  return <main className="dashboard"><SideNav name={data.user.displayName} onLogout={() => void signOut()} /><section className="dashboard-main" id="overview"><header className="dash-topbar"><button className="mobile-menu" aria-label="Open navigation"><Menu /></button><div><p>{new Date().toLocaleDateString("en-CA", { weekday: "long", month: "long", day: "numeric" })}</p><h1>Welcome back, {firstName}.</h1></div><div className="top-actions"><button aria-label="Notifications"><Bell /></button><span className="workspace-label">CAD workspace</span></div></header><div className="demo-banner"><ShieldCheck /><span><b>Educational planning workspace</b> — Prices are manually entered and all projections are estimates, not financial advice.</span></div>{error && <p className="workspace-error" role="alert">{error}</p>}
    <section className="summary-grid"><article className="summary-card featured"><div className="card-label"><span>Total portfolio</span><CircleDollarSign /></div><strong>{currency(data.portfolio.totalMarketValue)}</strong><p>Across <b>{data.portfolio.accountCount}</b> account{data.portfolio.accountCount === 1 ? "" : "s"}</p><div className="sparkline"><i /><i /><i /><i /><i /><i /><i /><i /><i /><i /><i /></div></article><article className="summary-card"><div className="card-label"><span>Portfolio positions</span><WalletCards /></div><strong>{data.portfolio.holdingCount}</strong><p>Manually priced holding{data.portfolio.holdingCount === 1 ? "" : "s"}</p><div className="progress"><span style={{ width: `${Math.min(100, data.portfolio.holdingCount * 12)}%` }} /></div><small>Add holdings to build the allocation view</small></article><article className="summary-card"><div className="card-label"><span>Primary goal</span><Target /></div><strong>{primaryGoal?.name ?? "Set a goal"}</strong><p>{primaryGoal ? <><b>{currency(primaryGoal.currentAmount)}</b> of {currency(primaryGoal.targetAmount)}</> : "Model a target and contribution"}</p><div className="progress gold"><span style={{ width: `${primaryGoal?.progressPercentage ?? 0}%` }} /></div><small>{primaryGoal ? `${primaryGoal.progressPercentage}% funded` : "No active goal"}</small></article></section>
    <section className="dashboard-content"><article className="dash-panel allocation-panel"><div className="panel-title"><div><p>Live portfolio</p><h2>Asset allocation</h2></div><a className="quiet-button" href="#planning">View plan</a></div>{allocation.length ? <div className="allocation-content"><div className="donut"><ResponsiveContainer width="100%" height="100%" initialDimension={{ width: 220, height: 220 }}><PieChart><Pie data={allocation} dataKey="percentage" innerRadius={67} outerRadius={91} paddingAngle={2} stroke="none">{allocation.map(item => <Cell key={item.assetClass} fill={item.color} />)}</Pie><Tooltip contentStyle={{ background: "#14201c", border: "1px solid #2a3a34", borderRadius: 8, color: "#eef4f1" }} formatter={value => `${Number(value).toFixed(1)}%`} /></PieChart></ResponsiveContainer><div><strong>{data.portfolio.holdingCount}</strong><span>positions</span></div></div><div className="allocation-list">{allocation.map(item => <div key={item.assetClass}><i style={{ background: item.color }} /><span>{item.name}</span><b>{item.percentage}%</b></div>)}</div></div> : <div className="empty-state compact-empty"><h3>No allocation yet</h3><p>Add a holding to calculate market value and allocation.</p></div>}</article><article className="dash-panel risk-panel"><div className="panel-title"><div><p>Risk assessment</p><h2>{data.latestRiskAssessment?.riskProfile ?? "Not assessed"}</h2></div>{data.latestRiskAssessment && <span className="score-pill">{data.latestRiskAssessment.totalScore} / 100</span>}</div><div className="risk-meter"><i /><span style={{ left: `${data.latestRiskAssessment?.totalScore ?? 50}%` }} /></div><div className="risk-labels"><span>Conservative</span><span>Balanced</span><span>Growth</span></div><p className="risk-copy">{data.latestRiskAssessment?.rationale ?? "Complete seven explainable questions to select a generic model allocation."}</p><a className="outline-button full" href="#risk">{data.latestRiskAssessment ? "Review assessment" : "Start assessment"}</a><small>Educational model · Not investment advice</small></article></section>
    <PortfolioSection accounts={data.accounts} onChanged={load} />
    <RiskSection assessment={data.latestRiskAssessment} onChanged={load} />
    <PlanningSection comparison={data.allocationComparison} goals={data.goals} onChanged={load} />
  </section></main>;
}
