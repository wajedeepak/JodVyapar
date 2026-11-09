import type { BadgeVariant } from '../../shared/ui/badge/badge';
import { Product, PublicProduct, PublicRequirement, Requirement } from './listing.models';

/** One line of a listing card: an icon, and text that says what it means. */
export interface ListingFact {
  icon: string;
  text: string;
}

const rupees = new Intl.NumberFormat('en-IN', {
  style: 'currency',
  currency: 'INR',
  minimumFractionDigits: 0,
  maximumFractionDigits: 2,
});
const number = new Intl.NumberFormat('en-IN', { maximumFractionDigits: 2 });
const dayMonthYear: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'short', year: 'numeric' };

/** "₹100 – ₹150", "₹100", or null when neither value is set. */
export function formatMoneyRange(min: number | null, max: number | null): string | null {
  if (min !== null && max !== null && min !== max) {
    return `${rupees.format(min)} – ${rupees.format(max)}`;
  }
  const single = min ?? max;
  return single === null ? null : rupees.format(single);
}

export function formatQuantity(quantity: number, unitSymbol: string): string {
  return `${number.format(quantity)} ${unitSymbol}`;
}

/** An API timestamp as "9 Jan 2027". */
export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-IN', dayMonthYear);
}

/** A yyyy-MM-dd date as "9 Jan 2027". Formatted in UTC so the day can't shift with the viewer's time zone. */
export function formatDateOnly(date: string): string {
  return new Date(`${date}T00:00:00Z`).toLocaleDateString('en-IN', { ...dayMonthYear, timeZone: 'UTC' });
}

export function productFacts(product: Product | PublicProduct): ListingFact[] {
  return [
    { icon: 'sell', text: `${formatMoneyRange(product.priceMin, product.priceMax)} / ${product.unitSymbol}` },
    { icon: 'inventory_2', text: `Minimum order ${formatQuantity(product.minOrderQty, product.unitSymbol)}` },
    { icon: 'place', text: `${product.cityName}, ${product.stateName}` },
  ];
}

/** Facts shared by the owner's and the public view of a requirement. */
export function requirementFacts(requirement: Requirement | PublicRequirement): ListingFact[] {
  const budget = formatMoneyRange(requirement.budgetMin, requirement.budgetMax);
  return [
    { icon: 'shopping_cart', text: `Needs ${formatQuantity(requirement.quantity, requirement.unitSymbol)}` },
    ...(budget ? [{ icon: 'payments', text: `Budget ${budget}` }] : []),
    { icon: 'place', text: `${requirement.cityName}, ${requirement.stateName}` },
    ...(requirement.neededBy ? [{ icon: 'event', text: `Needed by ${formatDateOnly(requirement.neededBy)}` }] : []),
  ];
}

export const STATUS_BADGES: Record<string, { label: string; variant: BadgeVariant }> = {
  Active: { label: 'Active', variant: 'success' },
  Paused: { label: 'Paused', variant: 'warning' },
  Fulfilled: { label: 'Fulfilled', variant: 'info' },
  Expired: { label: 'Expired', variant: 'neutral' },
  Removed: { label: 'Removed', variant: 'neutral' },
};
