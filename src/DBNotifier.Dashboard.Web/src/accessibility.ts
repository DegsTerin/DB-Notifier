/** Module purpose: Implements accessibility for the provider-neutral DB-Notifier Dashboard without direct database access. */
function channelToLinear(channel: number): number {
  const value = channel / 255;
  return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
}

export function relativeLuminance(hex: string): number {
  if (!/^#[0-9a-f]{6}$/i.test(hex)) throw new Error("Colour must use six-digit hexadecimal notation.");
  const channels = [1, 3, 5].map((offset) => Number.parseInt(hex.slice(offset, offset + 2), 16));
  return 0.2126 * channelToLinear(channels[0]) + 0.7152 * channelToLinear(channels[1]) + 0.0722 * channelToLinear(channels[2]);
}

export function contrastRatio(foreground: string, background: string): number {
  const values = [relativeLuminance(foreground), relativeLuminance(background)].sort((a, b) => b - a);
  return (values[0] + 0.05) / (values[1] + 0.05);
}
