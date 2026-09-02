"use client";

import { FormEvent, useEffect, useState } from "react";
import { Gauge, ShieldCheck } from "lucide-react";
import { apiFetch } from "../lib/api";
import type { RiskAssessment, RiskQuestion } from "../lib/types";

export default function RiskSection({ assessment, onChanged }: { assessment?: RiskAssessment; onChanged: () => Promise<void> }) {
  const [questions, setQuestions] = useState<RiskQuestion[]>([]);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [open, setOpen] = useState(!assessment);

  useEffect(() => {
    apiFetch<RiskQuestion[]>("/api/risk/questions").then(setQuestions)
      .catch(reason => setError(reason instanceof Error ? reason.message : "Could not load the questions."));
  }, []);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError("");
    try {
      await apiFetch("/api/risk/assessments", { method: "POST", body: JSON.stringify({ answers }) });
      await onChanged(); setOpen(false);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Could not score the assessment."); }
    finally { setBusy(false); }
  }

  return <section id="risk" className="dash-panel risk-workspace">
    <div className="panel-title"><div><p>Versioned educational model</p><h2>Risk assessment</h2></div>{assessment && <span className="score-pill">{assessment.totalScore} / 100</span>}</div>
    {assessment && <div className="risk-result"><div className="risk-result-heading"><span><Gauge /></span><div><small>Current profile</small><h3>{assessment.riskProfile}</h3></div></div><div className="risk-meter"><i /><span style={{ left: `${assessment.totalScore}%` }} /></div><div className="risk-labels"><span>Conservative</span><span>Balanced</span><span>Growth</span></div><p>{assessment.rationale}</p><small><ShieldCheck /> {assessment.disclaimer}</small><button type="button" className="outline-button" onClick={() => setOpen(value => !value)}>{open ? "Hide questionnaire" : "Retake assessment"}</button></div>}
    {open && <form className="risk-form" onSubmit={submit}>{questions.map((question, index) => <fieldset key={question.id}><legend><span>{String(index + 1).padStart(2, "0")}</span>{question.prompt}<small>{question.category}</small></legend><div className="option-grid">{question.options.map(option => <label key={option.id} className={answers[question.id] === option.id ? "selected" : ""}><input type="radio" name={question.id} value={option.id} required checked={answers[question.id] === option.id} onChange={() => setAnswers(current => ({ ...current, [question.id]: option.id }))} /><span>{option.label}</span><small>+{option.score}</small></label>)}</div></fieldset>)}{error && <p className="workspace-error" role="alert">{error}</p>}<button className="button" disabled={busy || questions.length === 0}>{busy ? "Calculating…" : "Calculate my profile"}</button></form>}
  </section>;
}
