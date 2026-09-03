import assert from "node:assert/strict";
import test from "node:test";

import mobileNavigation from "../app/dashboard/mobileNavigation.json" with { type: "json" };

test("mobile navigation exposes every required dashboard destination", () => {
  assert.deepEqual(mobileNavigation.items, [
    { label: "Dashboard", href: "#overview" },
    { label: "Portfolio", href: "#portfolio" },
    { label: "Transactions", href: "#transactions" },
    { label: "Asset Allocation", href: "#allocation" },
    { label: "Risk Assessment", href: "#risk" },
    { label: "Goals", href: "#goals" },
  ]);
});

test("mobile navigation destinations are unique in-page anchors", () => {
  const destinations = mobileNavigation.items.map(item => item.href);

  assert.equal(new Set(destinations).size, destinations.length);
  assert.ok(destinations.every(destination => destination.startsWith("#")));
});

test("mobile navigation exposes logout", () => {
  assert.equal(mobileNavigation.logoutLabel, "Logout");
});
