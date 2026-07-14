/** Module purpose: Renders recognisable provider marks for demonstration data without introducing provider logic into the neutral core. */

export type ProviderIconName = "postgresql" | "mysql" | "sql-server" | "oracle" | "mongodb" | string;

/**
 * Renders a compact, labelled provider glyph beside a visible provider name.
 * @param providerType - Stable provider identifier from the presentation contract.
 * @param className - Optional presentation class supplied by the owning view.
 * @returns A decorative SVG; adjacent text remains the authoritative accessible label.
 */
export function ProviderIcon({ providerType, className = "" }: { providerType: ProviderIconName; className?: string }) {
  const classes = `provider-icon provider-${providerType} ${className}`.trim();
  if (providerType === "postgresql") {
    return <svg className={classes} viewBox="0 0 32 32" aria-hidden="true" focusable="false"><path d="M8 7.2c2.6-3.2 12.8-3.2 15.7.4 2 2.5 1 8.3-.8 12.4-1.1 2.5-3.1 5.7-5.4 5.3-1.7-.3-1.3-3.1-1.5-5.2-.2-1.8-.8-3.2-2.2-4.3-1.9-1.5-4.7-1.1-6.2-3.1-1.2-1.6-.8-4.1.4-5.5Z"/><path d="M18.3 9.8c2.2 1.6 2.5 5.1.9 7.1-1.4 1.7-4.4 1.7-5.9.3M21.4 21.3c-1.3.3-2.7.1-3.8-.7"/></svg>;
  }
  if (providerType === "mysql") {
    return <svg className={classes} viewBox="0 0 32 32" aria-hidden="true" focusable="false"><path d="M4 18.8c4.7-6.6 10.8-8.1 18.3-4.4 2.8 1.4 4.6 3.5 5.7 6.3-4.3-2.6-8.3-2.9-12.1-.8-3.3 1.8-6.7 1.4-11.9-1.1Z"/><path d="M19.4 12.9c.4-2.1 1.6-4.1 3.5-5.8M11.6 17.6c2 .5 3.9 1.5 5.6 3"/></svg>;
  }
  if (providerType === "sql-server") {
    return <svg className={classes} viewBox="0 0 32 32" aria-hidden="true" focusable="false"><path d="M6 7c5.6 1.4 10.9 4 16 7.9-4.5 1.6-8.8 4.6-13 9.1 1.5-5.1 1.1-10.8-3-17Z"/><path d="M11 5c5.3 1.2 10.3 3.6 15 7.2-3.5.8-6.8 2.1-9.9 4"/></svg>;
  }
  if (providerType === "oracle") {
    return <svg className={classes} viewBox="0 0 32 32" aria-hidden="true" focusable="false"><rect x="4" y="10" width="24" height="12" rx="6"/></svg>;
  }
  if (providerType === "mongodb") {
    return <svg className={classes} viewBox="0 0 32 32" aria-hidden="true" focusable="false"><path d="M16 3c6 5.2 8.5 10.5 6.1 16-1.2 2.8-3.2 5.1-6.1 7-3-1.9-5-4.2-6.1-7C7.5 13.5 10 8.2 16 3Z"/><path d="M16 7v21"/></svg>;
  }
  return <svg className={classes} viewBox="0 0 32 32" aria-hidden="true" focusable="false"><ellipse cx="16" cy="8" rx="10" ry="4"/><path d="M6 8v8c0 2.2 4.5 4 10 4s10-1.8 10-4V8M6 16v8c0 2.2 4.5 4 10 4s10-1.8 10-4v-8"/></svg>;
}
