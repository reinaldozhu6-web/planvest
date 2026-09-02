import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "PlanVest — Portfolio clarity",
  description: "Educational portfolio planning, risk assessment, and goal tracking.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body>{children}</body></html>;
}
