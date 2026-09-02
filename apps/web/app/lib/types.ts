export type AccountType = "Tfsa" | "Rrsp" | "Fhsa" | "NonRegistered" | "Cash";
export type AssetClass = "CanadianEquity" | "UsEquity" | "InternationalEquity" | "FixedIncome" | "Cash" | "Other";
export type TransactionType = "Buy" | "Sell" | "Deposit" | "Withdrawal";
export type RiskProfile = "Conservative" | "Balanced" | "Growth";
export type GoalType = "EmergencyFund" | "Home" | "Education" | "Retirement" | "MajorPurchase" | "Other";
export type GoalStatus = "Active" | "Archived";

export interface User { id: string; displayName: string; email: string; }
export interface AuthResponse { accessToken: string; expiresAt: string; user: User; }

export interface Holding {
  id: string;
  investmentAccountId: string;
  symbol: string;
  assetName: string;
  assetClass: AssetClass;
  quantity: number;
  averageCost: number;
  currentPrice: number;
  marketValue: number;
  updatedAt: string;
}

export interface PortfolioTransaction {
  id: string;
  investmentAccountId: string;
  holdingId?: string;
  type: TransactionType;
  quantity: number;
  price: number;
  amount: number;
  transactionDate: string;
  note?: string;
}

export interface Account {
  id: string;
  name: string;
  accountType: AccountType;
  baseCurrency: string;
  marketValue: number;
  createdAt: string;
  holdings: Holding[];
  transactions: PortfolioTransaction[];
}

export interface AllocationItem { assetClass: AssetClass; marketValue: number; percentage: number; }
export interface PortfolioSummary { totalMarketValue: number; accountCount: number; holdingCount: number; allocation: AllocationItem[]; }

export interface RiskOption { id: string; label: string; score: number; }
export interface RiskQuestion { id: string; category: string; prompt: string; options: RiskOption[]; }
export interface RiskAssessment {
  id: string;
  scoringVersion: string;
  totalScore: number;
  riskProfile: RiskProfile;
  categorySubscores: Record<string, number>;
  rationale: string;
  createdAt: string;
  disclaimer: string;
}

export interface ModelAllocation { riskProfile: RiskProfile; equity: number; fixedIncome: number; cash: number; }
export interface AllocationComparisonItem {
  assetClass: string;
  currentPercentage: number;
  targetPercentage: number;
  differencePercentagePoints: number;
  approximateDollarGap: number;
}
export interface AllocationComparison { model: ModelAllocation; items: AllocationComparisonItem[]; disclaimer: string; }

export interface Goal {
  id: string;
  name: string;
  goalType: GoalType;
  targetAmount: number;
  currentAmount: number;
  targetDate: string;
  monthlyContribution: number;
  assumedAnnualReturn: number;
  status: GoalStatus;
  progressPercentage: number;
  projectedValue: number;
  requiredMonthlyContribution: number;
}

export interface DashboardData {
  user: User;
  portfolio: PortfolioSummary;
  accounts: Account[];
  latestRiskAssessment?: RiskAssessment;
  allocationComparison: AllocationComparison;
  goals: Goal[];
}
