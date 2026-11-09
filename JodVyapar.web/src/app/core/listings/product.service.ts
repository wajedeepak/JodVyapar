import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import { LISTING_PAGE_SIZE, PagedResult, Product, ProductRequest, ProductStatus, PublicProduct } from './listing.models';

/** The API returns image URLs from its own root; in production that is a different origin from the app. */
function absoluteImages<T extends { images: Product['images'] }>(listing: T): T {
  const origin = new URL(environment.apiBaseUrl, window.location.href).origin;
  return { ...listing, images: listing.images.map((image) => ({ ...image, url: `${origin}${image.url}` })) };
}

function absolutePage<T extends { images: Product['images'] }>(page: PagedResult<T>): PagedResult<T> {
  return { ...page, items: page.items.map(absoluteImages) };
}

/** /api/v1/products (Seller role) and the public product list of a profile. */
@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/products`;

  getMine(page: number, pageSize = LISTING_PAGE_SIZE, status?: ProductStatus): Observable<PagedResult<Product>> {
    return this.http
      .get<PagedResult<Product>>(`${this.baseUrl}/mine`, { params: { page, pageSize, ...(status ? { status } : {}) } })
      .pipe(map(absolutePage));
  }

  /** How many of the seller's products are live. */
  countActive(): Observable<number> {
    return this.getMine(1, 1, 'Active').pipe(map((page) => page.totalCount));
  }

  get(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`).pipe(map(absoluteImages));
  }

  create(request: ProductRequest): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, request).pipe(map(absoluteImages));
  }

  update(id: string, request: ProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/${id}`, request).pipe(map(absoluteImages));
  }

  pause(id: string): Observable<Product> {
    return this.http.post<Product>(`${this.baseUrl}/${id}/pause`, null).pipe(map(absoluteImages));
  }

  activate(id: string): Observable<Product> {
    return this.http.post<Product>(`${this.baseUrl}/${id}/activate`, null).pipe(map(absoluteImages));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  addImage(id: string, file: File): Observable<Product> {
    const body = new FormData();
    body.append('file', file, file.name);
    return this.http.post<Product>(`${this.baseUrl}/${id}/images`, body).pipe(map(absoluteImages));
  }

  removeImage(id: string, imageId: string): Observable<Product> {
    return this.http.delete<Product>(`${this.baseUrl}/${id}/images/${imageId}`).pipe(map(absoluteImages));
  }

  /** Active products on a public profile. */
  getPublic(userId: string, page: number, pageSize = LISTING_PAGE_SIZE): Observable<PagedResult<PublicProduct>> {
    return this.http
      .get<PagedResult<PublicProduct>>(`${environment.apiBaseUrl}/profile/${encodeURIComponent(userId)}/products`, {
        params: { page, pageSize },
      })
      .pipe(map(absolutePage));
  }
}
