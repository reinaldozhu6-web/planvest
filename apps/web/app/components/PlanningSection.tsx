"use client";

import { FormEvent, useState } from "react";
import { Archive, ArchiveRestore, Edit3, Target, Trash2, TrendingUp } from "lucide-react";
import { apiFetch, currency, label } from "../lib/api";
import type { AllocationComparison, Goal, GoalType } from "../lib/types";

const goalTypes: GoalType[] = ["EmergencyFund", "Home", "Education", "Retirement", "MajorPurchase", "Other"];

export default function PlanningSection({ comparison, goals, onChanged }: { comparison: AllocationComparison; goals: Goal[]; onChanged: () => Promise<void> }) {
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [showGoalForm, setShowGoalForm] = useState(goals.length === 0);
  const [editingGoal, setEditingGoal] = useState<Goal | null>(null);
  const [projection, setProjection] = useState<{ future: number; required: number } | null>(null);
  const [tomorrow] = useState(() => new Date(Date.now() + 86400000).toISOString().slice(0, 10));

  async function run(action: () => Promise<unknown>): Promise<boolean> {
    setBusy(true); setError("");
    try { await action(); await onChanged(); return true; }
    catch (reason) { setError(reason instanceof Error ? reason.message : "The change could not be saved."); return false; }
    finally { setBusy(false); }
  }

  async function saveGoal(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form);
    const body = JSON.stringify({
      name: data.get("name"), goalType: data.get("goalType"), targetAmount: Number(data.get("targetAmount")),
      currentAmount: Number(data.get("currentAmount")), targetDate: data.get("targetDate"),
      monthlyContribution: Number(data.get("monthlyContribution")), assumedAnnualReturn: Number(data.get("assumedAnnualReturn")), status: editingGoal?.status ?? "Active",
    });
    const saved = await run(() => apiFetch(editingGoal ? `/api/goals/${editingGoal.id}` : "/api/goals", { method: editingGoal ? "PUT" : "POST", body }));
    if (saved) { setEditingGoal(null); setShowGoalForm(false); form.reset(); }
  }

  async function simulate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError(""); const data = new FormData(event.currentTarget);
    const principal = Number(data.get("principal")); const monthlyContribution = Number(data.get("monthlyContribution"));
    const annualRatePercent = Number(data.get("annualRatePercent")); const months = Number(data.get("months")); const target = Number(data.get("target"));
    try {
      const [future, required] = await Promise.all([
        apiFetch<{ value: number }>("/api/simulations/future-value", { method: "POST", body: JSON.stringify({ principal, monthlyContribution, annualRatePercent, months }) }),
        apiFetch<{ value: number }>("/api/simulations/required-contribution", { method: "POST", body: JSON.stringify({ target, principal, annualRatePercent, months }) }),
      ]);
      setProjection({ future: future.value, required: required.value });
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Could not calculate the projection."); }
    finally { setBusy(false); }
  }

  function beginEdit(goal: Goal) { setEditingGoal(goal); setShowGoalForm(true); }
  function toggleArchive(goal: Goal) {
    return run(() => apiFetch(`/api/goals/${goal.id}`, { method: "PUT", body: JSON.stringify({
      name: goal.name, goalType: goal.goalType, targetAmount: goal.targetAmount,
      currentAmount: goal.currentAmount, targetDate: goal.targetDate,
      monthlyContribution: goal.monthlyContribution, assumedAnnualReturn: goal.assumedAnnualReturn,
      status: goal.status === "Active" ? "Archived" : "Active",
    }) }));
  }

  return <section id="planning" className="planning-workspace">
    <article className="dash-panel comparison-panel"><div className="panel-title"><div><p>{comparison.model.riskProfile} model</p><h2>Allocation plan</h2></div><span className="score-pill">{comparison.model.equity}% equity</span></div><div className="responsive-table"><table><thead><tr><th>Asset class</th><th>Current</th><th>Target</th><th>Difference</th><th>Approx. gap</th></tr></thead><tbody>{comparison.items.map(item => <tr key={item.assetClass}><td><b>{item.assetClass}</b></td><td>{item.currentPercentage}%</td><td>{item.targetPercentage}%</td><td className={item.differencePercentagePoints >= 0 ? "positive" : "negative"}>{item.differencePercentagePoints > 0 ? "+" : ""}{item.differencePercentagePoints} pp</td><td>{currency(item.approximateDollarGap)}</td></tr>)}</tbody></table></div><p className="panel-disclaimer">{comparison.disclaimer}</p></article>
    <article id="goals" className="dash-panel goals-panel"><div className="panel-title"><div><p>Progress and projections</p><h2>Financial goals</h2></div><button type="button" className="button compact" onClick={() => { setEditingGoal(null); setShowGoalForm(value => !value); }}><Target /> Add goal</button></div>
      {error && <p className="workspace-error" role="alert">{error}</p>}
      {showGoalForm && <form className="inline-form goal-form" onSubmit={saveGoal}><label>Goal name<input name="name" required minLength={2} maxLength={100} defaultValue={editingGoal?.name} placeholder="Home deposit" /></label><label>Type<select name="goalType" defaultValue={editingGoal?.goalType ?? "Home"}>{goalTypes.map(type => <option key={type} value={type}>{label(type)}</option>)}</select></label><label>Target amount<input name="targetAmount" type="number" min="1" step="0.01" required defaultValue={editingGoal?.targetAmount} /></label><label>Saved now<input name="currentAmount" type="number" min="0" step="0.01" required defaultValue={editingGoal?.currentAmount ?? 0} /></label><label>Target date<input name="targetDate" type="date" min={tomorrow} required defaultValue={editingGoal?.targetDate ?? tomorrow} /></label><label>Monthly contribution<input name="monthlyContribution" type="number" min="0" step="0.01" required defaultValue={editingGoal?.monthlyContribution ?? 0} /></label><label>Assumed annual return %<input name="assumedAnnualReturn" type="number" min="0" max="30" step="0.1" required defaultValue={editingGoal?.assumedAnnualReturn ?? 4} /></label><button className="button compact" disabled={busy}>{editingGoal ? "Save goal" : "Create goal"}</button></form>}
      <div className="goal-grid">{goals.map(goal => <article className={`goal-card ${goal.status === "Archived" ? "archived" : ""}`} key={goal.id}><div className="goal-card-top"><span><Target /></span><div className="row-actions"><button type="button" aria-label={`${goal.status === "Active" ? "Archive" : "Restore"} ${goal.name}`} onClick={() => void toggleArchive(goal)}>{goal.status === "Active" ? <Archive /> : <ArchiveRestore />}</button><button type="button" aria-label={`Edit ${goal.name}`} onClick={() => beginEdit(goal)}><Edit3 /></button><button type="button" aria-label={`Delete ${goal.name}`} onClick={() => window.confirm(`Delete ${goal.name}?`) && run(() => apiFetch(`/api/goals/${goal.id}`, { method: "DELETE" }))}><Trash2 /></button></div></div><small>{label(goal.status)} · {label(goal.goalType)} · {new Date(`${goal.targetDate}T00:00:00`).toLocaleDateString("en-CA", { month: "short", year: "numeric" })}</small><h3>{goal.name}</h3><strong>{currency(goal.currentAmount)} <em>of {currency(goal.targetAmount)}</em></strong><div className="progress gold"><span style={{ width: `${goal.progressPercentage}%` }} /></div><div className="goal-meta"><span>{goal.progressPercentage}% funded</span><span>{currency(goal.monthlyContribution)}/mo</span></div><p>Projected: <b>{currency(goal.projectedValue)}</b></p></article>)}</div>
      {!goals.length && !showGoalForm && <div className="empty-state"><h3>No goals yet</h3><p>Create a target to see funded progress and the estimated monthly contribution.</p></div>}
    </article>
    <article id="simulator" className="dash-panel simulator-panel"><div className="panel-title"><div><p>Monthly compounding</p><h2>Contribution simulator</h2></div><TrendingUp /></div><form className="simulator-form" onSubmit={simulate}><label>Starting amount<input name="principal" type="number" min="0" step="0.01" required defaultValue="10000" /></label><label>Monthly contribution<input name="monthlyContribution" type="number" min="0" step="0.01" required defaultValue="500" /></label><label>Annual return %<input name="annualRatePercent" type="number" min="0" max="30" step="0.1" required defaultValue="6" /></label><label>Months<input name="months" type="number" min="1" max="600" required defaultValue="120" /></label><label>Target<input name="target" type="number" min="1" step="0.01" required defaultValue="100000" /></label><button className="button" disabled={busy}>Run projection</button></form>{projection && <div className="projection-result"><div><small>Projected value</small><strong>{currency(projection.future)}</strong></div><div><small>Required monthly contribution</small><strong>{currency(projection.required)}</strong></div></div>}<p className="panel-disclaimer">Projection based on your assumptions. Returns are not guaranteed.</p></article>
  </section>;
}
