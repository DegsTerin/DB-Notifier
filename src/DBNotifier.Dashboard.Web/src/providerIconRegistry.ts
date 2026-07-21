/**
 * Module purpose: Declares the local provider-icon catalogue used by the Dashboard presentation boundary.
 * Paths are fixed application assets; unknown provider identifiers remain valid and use the generic glyph in the owning component.
 */

import {
  providerIconRegistry,
  type ProviderIconAssets,
  type RegisteredProviderIconId,
} from "./generated/providerIconRegistry.ts";

export { providerIconRegistry, type ProviderIconAssets, type RegisteredProviderIconId };

/** The five provider-neutral categorical slots defined by the Design System. */
export type ProviderCategorySlot = 1 | 2 | 3 | 4 | 5;

/**
 * Resolves fixed local assets for an exact provider identifier.
 * @param providerType - Stable provider identifier received from the presentation contract.
 * @returns The registered Light and Dark paths, or undefined when the provider must use the generic fallback.
 */
export function resolveProviderIconAssets(providerType: string): ProviderIconAssets | undefined {
  if (!Object.prototype.hasOwnProperty.call(providerIconRegistry, providerType)) return undefined;
  return providerIconRegistry[providerType as RegisteredProviderIconId];
}

/**
 * Maps an ordinal provider position onto the bounded Design System category palette.
 * @param index - Zero-based position in the visible provider dataset.
 * @returns A stable slot from one to five; invalid positions fail safely to the first neutral category.
 */
export function resolveProviderCategorySlot(index: number): ProviderCategorySlot {
  if (!Number.isSafeInteger(index) || index < 0) return 1;
  return ((index % 5) + 1) as ProviderCategorySlot;
}
