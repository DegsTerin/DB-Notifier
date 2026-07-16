/** Module purpose: Verifies strict browser-local Dashboard routing without starting a browser or contacting external systems. */
import assert from "node:assert/strict";
import test from "node:test";
import {
  dashboardViewHash,
  dashboardViews,
  focusDashboardMain,
  parseDashboardView,
  subscribeDashboardRoute,
} from "../src/routing.ts";

test("route parser accepts every declared destination and emits canonical hashes", () => {
  for (const view of dashboardViews) {
    assert.equal(parseDashboardView(`#${view}`), view);
    assert.equal(parseDashboardView(view), view);
    assert.equal(dashboardViewHash(view), `#${view}`);
  }
});

test("route parser rejects inherited and unknown object properties", () => {
  for (const candidate of ["", "main-content", "missing", "toString", "constructor", "__proto__", "hasOwnProperty"]) {
    assert.equal(parseDashboardView(`#${candidate}`), "overview");
  }
});

test("route subscription restores allow-listed views after reload, Back and Forward navigation", () => {
  const listeners = new Map<string, () => void>();
  const target = {
    location: { hash: "#settings" },
    addEventListener(type: string, listener: () => void) { listeners.set(type, listener); },
    removeEventListener(type: string, listener: () => void) {
      if (listeners.get(type) === listener) listeners.delete(type);
    },
  };
  const observed: string[] = [parseDashboardView(target.location.hash)];
  const unsubscribe = subscribeDashboardRoute(target, (view) => observed.push(view));

  target.location.hash = "#history";
  listeners.get("popstate")?.();
  target.location.hash = "#inventory";
  listeners.get("hashchange")?.();
  target.location.hash = "#constructor";
  listeners.get("popstate")?.();

  assert.deepEqual(observed, ["settings", "history", "inventory", "overview"]);
  unsubscribe();
  assert.equal(listeners.size, 0);
});

test("skip-link focus preserves the active route hash", () => {
  let routeHash = "#providers";
  let prevented = false;
  let focussed = false;
  let scrolled = false;
  focusDashboardMain(
    { preventDefault() { prevented = true; } },
    {
      focus(options) {
        focussed = options?.preventScroll === true;
      },
      scrollIntoView(options) {
        scrolled = options?.block === "start";
      },
    },
  );

  assert.equal(prevented, true);
  assert.equal(focussed, true);
  assert.equal(scrolled, true);
  assert.equal(routeHash, "#providers");
  routeHash = "#providers";
});
