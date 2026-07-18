/**
 * Module purpose: Renders registered local provider artwork with theme-aware, generic and forced-colour fallbacks.
 * Every image is decorative because adjacent visible text remains the authoritative provider identity and support evidence.
 */
import { useState } from "react";
import { resolveProviderIconAssets } from "./providerIconRegistry";

export type ProviderIconName = string;

/**
 * Renders the provider-neutral database outline used for unknown, failed and forced-colour presentation.
 * @param className - Presentation classes that select the fallback's owning state.
 * @returns A decorative SVG whose colour remains controlled by semantic or system text colour.
 */
function GenericProviderGlyph({ className }: { className: string }) {
  return <svg className={className} viewBox="0 0 32 32" aria-hidden="true" focusable="false"><ellipse cx="16" cy="8" rx="10" ry="4"/><path d="M6 8v8c0 2.2 4.5 4 10 4s10-1.8 10-4V8M6 16v8c0 2.2 4.5 4 10 4s10-1.8 10-4v-8"/></svg>;
}

/**
 * Renders one fixed theme variant and replaces a failed local image with the generic outline.
 * @param source - Literal local asset path supplied by the declarative registry.
 * @param theme - Explicit theme whose CSS visibility this variant follows.
 * @returns A decorative local image or its generic failure fallback.
 */
function ProviderIconVariant({ source, theme }: { source: string; theme: "light" | "dark" }) {
  const [failed, setFailed] = useState(false);
  return <span className={`provider-icon-variant provider-icon-variant-${theme}`}>
    {failed
      ? <GenericProviderGlyph className="provider-icon-generic provider-icon-error-fallback" />
      : <img src={source} alt="" className="provider-icon-image" onError={() => setFailed(true)} />}
  </span>;
}

/**
 * Renders compact decorative provider artwork intended to sit beside a visible provider name.
 * @param providerType - Stable provider identifier from the presentation contract.
 * @param className - Optional presentation class supplied by the owning view.
 * @returns Decorative theme-specific artwork or a provider-neutral database outline.
 */
export function ProviderIcon({ providerType, className = "" }: { providerType: ProviderIconName; className?: string }) {
  const assets = resolveProviderIconAssets(providerType);
  return <span className={`provider-icon ${className}`.trim()} aria-hidden="true">
    {assets
      ? <>
          <ProviderIconVariant key={`light:${assets.lightPath}`} source={assets.lightPath} theme="light" />
          <ProviderIconVariant key={`dark:${assets.darkPath}`} source={assets.darkPath} theme="dark" />
          <GenericProviderGlyph className="provider-icon-generic provider-icon-forced-colours" />
        </>
      : <GenericProviderGlyph className="provider-icon-generic provider-icon-default-fallback" />}
  </span>;
}
