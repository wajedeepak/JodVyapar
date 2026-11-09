/** One page of a list from the API. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export const LISTING_PAGE_SIZE = 12;

export const TITLE_MAX_LENGTH = 120;
export const DESCRIPTION_MAX_LENGTH = 2000;
export const MAX_KEYWORDS = 10;
export const KEYWORD_MAX_LENGTH = 40;

export const MAX_PRODUCT_IMAGES = 3;
export const MAX_IMAGE_BYTES = 2 * 1024 * 1024;

export type ProductStatus = 'Active' | 'Paused' | 'Removed';
export type RequirementStatus = 'Active' | 'Paused' | 'Fulfilled' | 'Expired' | 'Removed';

export interface ProductImage {
  id: string;
  url: string;
  sortOrder: number;
}

/** The owner's view of a product. */
export interface Product {
  id: string;
  title: string;
  description: string | null;
  parentCategoryId: string;
  parentCategoryName: string;
  categoryId: string;
  categoryName: string;
  priceMin: number;
  priceMax: number | null;
  unitId: string;
  unitName: string;
  unitSymbol: string;
  minOrderQty: number;
  stateId: string;
  stateName: string;
  cityId: string;
  cityName: string;
  keywords: string[];
  status: ProductStatus;
  images: ProductImage[];
  createdAt: string;
  updatedAt: string;
}

/** Body of POST /products and PUT /products/{id}. */
export interface ProductRequest {
  title: string;
  description: string | null;
  categoryId: string | null;
  priceMin: number | null;
  priceMax: number | null;
  unitId: string;
  minOrderQty: number | null;
  stateId: string | null;
  cityId: string | null;
  keywords: string[];
}

/** A product on a public profile. Never has contact details. */
export interface PublicProduct {
  id: string;
  title: string;
  description: string | null;
  parentCategoryName: string;
  categoryName: string;
  priceMin: number;
  priceMax: number | null;
  unitSymbol: string;
  minOrderQty: number;
  stateName: string;
  cityName: string;
  keywords: string[];
  images: ProductImage[];
  createdAt: string;
}

/** The owner's view of a requirement. */
export interface Requirement {
  id: string;
  title: string;
  description: string | null;
  parentCategoryId: string;
  parentCategoryName: string;
  categoryId: string;
  categoryName: string;
  budgetMin: number | null;
  budgetMax: number | null;
  quantity: number;
  unitId: string;
  unitName: string;
  unitSymbol: string;
  stateId: string;
  stateName: string;
  cityId: string;
  cityName: string;
  keywords: string[];
  /** yyyy-MM-dd */
  neededBy: string | null;
  status: RequirementStatus;
  expiresAt: string;
  createdAt: string;
  updatedAt: string;
}

/** Body of POST /requirements and PUT /requirements/{id}. */
export interface RequirementRequest {
  title: string;
  description: string | null;
  categoryId: string | null;
  budgetMin: number | null;
  budgetMax: number | null;
  quantity: number | null;
  unitId: string;
  stateId: string | null;
  cityId: string | null;
  neededBy: string | null;
  keywords: string[];
}

/** A requirement on a public profile. Never has contact details. */
export interface PublicRequirement {
  id: string;
  title: string;
  description: string | null;
  parentCategoryName: string;
  categoryName: string;
  budgetMin: number | null;
  budgetMax: number | null;
  quantity: number;
  unitSymbol: string;
  stateName: string;
  cityName: string;
  keywords: string[];
  neededBy: string | null;
  expiresAt: string;
  createdAt: string;
}
