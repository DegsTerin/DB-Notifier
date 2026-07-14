/** Module purpose: Verifies that Dashboard timestamps follow the selected system time zone without changing their instants. */
import assert from "node:assert/strict";
import test from "node:test";
import { formatSystemDateTime } from "../src/systemDateTime.ts";

test("system time presentation converts UTC instants and exposes the local zone", () => {
  const instant = "2026-07-14T01:34:40.000Z";
  const local = formatSystemDateTime(instant, "pt-BR", "America/Sao_Paulo");

  assert.match(local, /13\/07\/2026/);
  assert.match(local, /22:34:40/);
  assert.doesNotMatch(local, /UTC/);
});

test("system time presentation preserves locale-specific date ordering", () => {
  const instant = "2026-07-14T01:34:40.000Z";
  const local = formatSystemDateTime(instant, "en-GB", "Europe/London");

  assert.match(local, /14\/07\/2026/);
  assert.match(local, /02:34:40/);
});

test("invalid instants fail explicitly instead of presenting fabricated time", () => {
  assert.throws(() => formatSystemDateTime("not-an-instant", "en-GB"), RangeError);
});
