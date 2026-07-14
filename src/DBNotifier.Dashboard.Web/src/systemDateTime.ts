/**
 * Formats Dashboard instants in the browser's current system time zone while preserving their exact ISO values.
 * The adapter is presentation-only and never changes canonical UTC timestamps or freshness calculations.
 */
import type { SupportedLocale } from "./generated/localisation";

/**
 * Formats an exact instant with the browser system time zone and an explicit short zone label.
 * @param value - ISO timestamp or Date containing the exact instant to present.
 * @param locale - Supported interface locale used for date and time ordering.
 * @param timeZone - Optional deterministic override for tests; omission uses the browser system time zone.
 * @returns The localised date, time and short time-zone label.
 * @throws {RangeError} When the instant or supplied time zone is invalid.
 */
export function formatSystemDateTime(
  value: string | Date,
  locale: SupportedLocale,
  timeZone?: string,
): string {
  return new Intl.DateTimeFormat(locale, {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    timeZoneName: "short",
    ...(timeZone ? { timeZone } : {}),
  }).format(typeof value === "string" ? new Date(value) : value);
}
