/**
 * Locally the Angular dev server talks to the ASP.NET API on port 5000.
 * In production Netlify proxies /api/* and /uploads/* to the hosted API
 * (see frontend/public/_redirects), so a relative base URL is used.
 */
const isLocal = typeof window !== 'undefined' &&
  (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1');

export const API = isLocal ? 'http://localhost:5000/api' : '/api';
export const UPLOADS = isLocal ? 'http://localhost:5000' : '';

/** Resolve API-stored image paths against the API origin, leaving external URLs alone. */
export function assetUrl(path: string): string {
  if (!path) return '';
  if (/^(https?:|data:|blob:)/i.test(path)) return path;
  return `${UPLOADS}${path.startsWith('/') ? path : `/${path}`}`;
}
