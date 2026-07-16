/**
 * Module purpose: Renders one locally defined generic database outline without importing or approximating vendor artwork.
 * The decorative glyph uses one non-status categorical colour; visible text remains the authoritative provider identity and support evidence.
 */

export type ProviderIconName = string;

/**
 * Renders a compact, labelled provider glyph beside a visible provider name.
 * @param providerType - Stable provider identifier from the presentation contract.
 * @param className - Optional presentation class supplied by the owning view.
 * @returns A decorative SVG; adjacent text remains the authoritative accessible label.
 */
export function ProviderIcon({ providerType, className = "" }: { providerType: ProviderIconName; className?: string }) {
  void providerType;
  return <svg className={`provider-icon ${className}`.trim()} viewBox="0 0 32 32" aria-hidden="true" focusable="false"><ellipse cx="16" cy="8" rx="10" ry="4"/><path d="M6 8v8c0 2.2 4.5 4 10 4s10-1.8 10-4V8M6 16v8c0 2.2 4.5 4 10 4s10-1.8 10-4v-8"/></svg>;
}
