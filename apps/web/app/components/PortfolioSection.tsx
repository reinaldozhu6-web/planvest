"use client";

import { FormEvent, useMemo, useState } from "react";
import { Edit3, Plus, Trash2 } from "lucide-react";
import { apiFetch, currency, label } from "../lib/api";
import type { Account, AccountType, AssetClass, Holding, TransactionType } from "../lib/types";

const accountTypes: AccountType[] = ["Tfsa", "Rrsp", "Fhsa", "NonRegistered", "Cash"];
const assetClasses: AssetClass[] = ["CanadianEquity", "UsEquity", "InternationalEquity", "FixedIncome", "Cash", "Other"];
const transactionTypes: TransactionType[] = ["Buy", "Sell", "Deposit", "Withdrawal"];

export default function PortfolioSection({ accounts, onChanged }: { accounts: Account[]; onChanged: () => Promise<void> }) {
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [showAccountForm, setShowAccountForm] = useState(accounts.length === 0);
  const [editingAccount, setEditingAccount] = useState<Account | null>(null);
  const [editingHolding, setEditingHolding] = useState<Holding | null>(null);
  const [holdingAccountId, setHoldingAccountId] = useState(accounts[0]?.id ?? "");
  const [showHoldingForm, setShowHoldingForm] = useState(false);

  const allHoldings = useMemo(() => accounts.flatMap(account => account.holdings.map(holding => ({ account, holding }))), [accounts]);
  const allTransactions = useMemo(() => accounts.flatMap(account => account.transactions.map(transaction => ({ account, transaction })))
    .sort((left, right) => right.transaction.transactionDate.localeCompare(left.transaction.transactionDate)), [accounts]);
  const selectedHoldingAccount = holdingAccountId || accounts[0]?.id || "";

  async function run(action: () => Promise<unknown>): Promise<boolean> {
    setBusy(true); setError("");
    try { await action(); await onChanged(); return true; }
    catch (reason) { setError(reason instanceof Error ? reason.message : "The change could not be saved."); return false; }
    finally { setBusy(false); }
  }

  async function saveAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form);
    const body = JSON.stringify({ name: data.get("name"), accountType: data.get("accountType") });
    const saved = await run(() => apiFetch(editingAccount ? `/api/accounts/${editingAccount.id}` : "/api/accounts", {
      method: editingAccount ? "PUT" : "POST", body,
    }));
    if (saved) { setEditingAccount(null); setShowAccountForm(false); form.reset(); }
  }

  async function saveHolding(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form);
    const accountId = String(data.get("accountId"));
    const body = JSON.stringify({
      symbol: data.get("symbol"), assetName: data.get("assetName"), assetClass: data.get("assetClass"),
      quantity: Number(data.get("quantity")), averageCost: Number(data.get("averageCost")), currentPrice: Number(data.get("currentPrice")),
    });
    const saved = await run(() => apiFetch(editingHolding ? `/api/holdings/${editingHolding.id}` : `/api/accounts/${accountId}/holdings`, {
      method: editingHolding ? "PUT" : "POST", body,
    }));
    if (saved) { setEditingHolding(null); setShowHoldingForm(false); form.reset(); }
  }

  async function saveTransaction(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form);
    const accountId = String(data.get("accountId"));
    const saved = await run(() => apiFetch(`/api/accounts/${accountId}/transactions`, {
      method: "POST", body: JSON.stringify({
        type: data.get("type"), holdingId: data.get("holdingId") || null,
        quantity: Number(data.get("quantity") || 0), price: Number(data.get("price") || 0),
        amount: Number(data.get("amount") || 0), transactionDate: data.get("transactionDate"), note: data.get("note") || null,
      }),
    }));
    if (saved) form.reset();
  }

  function beginAccountEdit(account: Account) { setEditingAccount(account); setShowAccountForm(true); }
  function beginHoldingEdit(holding: Holding) { setEditingHolding(holding); setHoldingAccountId(holding.investmentAccountId); setShowHoldingForm(true); }

  return <section id="portfolio" className="dash-panel workspace-panel">
    <div className="panel-title"><div><p>Owned resources</p><h2>Accounts and holdings</h2></div><div className="panel-actions"><button type="button" className="quiet-button" onClick={() => { setEditingAccount(null); setShowAccountForm(value => !value); }}><Plus /> Account</button><button type="button" className="button compact" disabled={!accounts.length} onClick={() => { setEditingHolding(null); setShowHoldingForm(value => !value); }}><Plus /> Holding</button></div></div>
    {error && <p className="workspace-error" role="alert">{error}</p>}
    {showAccountForm && <form className="inline-form" onSubmit={saveAccount}><label>Account name<input name="name" required minLength={2} maxLength={80} defaultValue={editingAccount?.name} placeholder="Long-term TFSA" /></label><label>Type<select name="accountType" defaultValue={editingAccount?.accountType ?? "Tfsa"}>{accountTypes.map(type => <option key={type} value={type}>{label(type)}</option>)}</select></label><button className="button compact" disabled={busy}>{editingAccount ? "Save account" : "Add account"}</button></form>}
    {showHoldingForm && <form className="inline-form holding-form" onSubmit={saveHolding}>
      <label>Account<select name="accountId" value={selectedHoldingAccount} disabled={Boolean(editingHolding)} onChange={event => setHoldingAccountId(event.target.value)}>{accounts.map(account => <option key={account.id} value={account.id}>{account.name}</option>)}</select></label>
      <label>Symbol<input name="symbol" required maxLength={20} defaultValue={editingHolding?.symbol} placeholder="XEQT" /></label>
      <label>Asset name<input name="assetName" required minLength={2} maxLength={120} defaultValue={editingHolding?.assetName} placeholder="Core equity ETF" /></label>
      <label>Class<select name="assetClass" defaultValue={editingHolding?.assetClass ?? "CanadianEquity"}>{assetClasses.map(value => <option key={value} value={value}>{label(value)}</option>)}</select></label>
      <label>Quantity<input name="quantity" type="number" min="0.000001" step="0.000001" required defaultValue={editingHolding?.quantity} /></label>
      <label>Average cost<input name="averageCost" type="number" min="0" step="0.01" required defaultValue={editingHolding?.averageCost} /></label>
      <label>Current price<input name="currentPrice" type="number" min="0" step="0.01" required defaultValue={editingHolding?.currentPrice} /></label>
      <button className="button compact" disabled={busy}>{editingHolding ? "Save holding" : "Add holding"}</button>
    </form>}
    <div className="account-strip">{accounts.map(account => <article className="account-card" key={account.id}><div><span>{label(account.accountType)}</span><h3>{account.name}</h3><strong>{currency(account.marketValue)}</strong><small>{account.holdings.length} holding{account.holdings.length === 1 ? "" : "s"}</small></div><div className="row-actions"><button type="button" aria-label={`Edit ${account.name}`} onClick={() => beginAccountEdit(account)}><Edit3 /></button><button type="button" aria-label={`Delete ${account.name}`} onClick={() => window.confirm(`Delete ${account.name} and its holdings?`) && run(() => apiFetch(`/api/accounts/${account.id}`, { method: "DELETE" }))}><Trash2 /></button></div></article>)}</div>
    {!accounts.length && <div className="empty-state"><h3>Build your first portfolio</h3><p>Add an account, then enter a holding with a manually supplied current price.</p></div>}
    {allHoldings.length > 0 && <div className="responsive-table"><table><thead><tr><th>Holding</th><th>Account</th><th>Asset class</th><th>Quantity</th><th>Price</th><th>Market value</th><th><span className="sr-only">Actions</span></th></tr></thead><tbody>{allHoldings.map(({ account, holding }) => <tr key={holding.id}><td><div className="holding-name"><span>{holding.symbol.slice(0, 2)}</span><div><b>{holding.symbol}</b><small>{holding.assetName}</small></div></div></td><td><span className="tag">{account.name}</span></td><td>{label(holding.assetClass)}</td><td>{holding.quantity.toLocaleString()}</td><td>{currency(holding.currentPrice)}</td><td><b>{currency(holding.marketValue)}</b></td><td><div className="row-actions"><button type="button" aria-label={`Edit ${holding.symbol}`} onClick={() => beginHoldingEdit(holding)}><Edit3 /></button><button type="button" aria-label={`Delete ${holding.symbol}`} onClick={() => window.confirm(`Delete ${holding.symbol}?`) && run(() => apiFetch(`/api/holdings/${holding.id}`, { method: "DELETE" }))}><Trash2 /></button></div></td></tr>)}</tbody></table></div>}
    {accounts.length > 0 && <details className="transaction-entry"><summary>Record a transaction</summary><form className="inline-form transaction-form" onSubmit={saveTransaction}><label>Account<select name="accountId" required>{accounts.map(account => <option key={account.id} value={account.id}>{account.name}</option>)}</select></label><label>Type<select name="type">{transactionTypes.map(type => <option key={type}>{type}</option>)}</select></label><label>Holding (for trades)<select name="holdingId"><option value="">Not applicable</option>{allHoldings.map(({ holding }) => <option key={holding.id} value={holding.id}>{holding.symbol}</option>)}</select></label><label>Quantity<input name="quantity" type="number" min="0" step="0.000001" /></label><label>Price<input name="price" type="number" min="0" step="0.01" /></label><label>Cash amount<input name="amount" type="number" min="0" step="0.01" /></label><label>Date<input name="transactionDate" type="date" required defaultValue={new Date().toISOString().slice(0, 10)} /></label><label>Note<input name="note" maxLength={240} /></label><button className="button compact" disabled={busy}>Record</button></form></details>}
    {allTransactions.length > 0 && <div className="transaction-history"><h3>Recent transactions</h3><div className="responsive-table"><table><thead><tr><th>Date</th><th>Type</th><th>Account</th><th>Holding</th><th>Amount</th><th>Note</th></tr></thead><tbody>{allTransactions.slice(0, 12).map(({ account, transaction }) => <tr key={transaction.id}><td>{new Date(`${transaction.transactionDate}T00:00:00`).toLocaleDateString("en-CA")}</td><td><span className="tag">{label(transaction.type)}</span></td><td>{account.name}</td><td>{account.holdings.find(holding => holding.id === transaction.holdingId)?.symbol ?? "—"}</td><td><b>{currency(transaction.amount)}</b></td><td>{transaction.note || "—"}</td></tr>)}</tbody></table></div></div>}
  </section>;
}
