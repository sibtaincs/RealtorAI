/**
 * Locally the Angular dev server talks to the ASP.NET API on port 5000.
 * In production the app calls the hosted Render API directly. The Render service
 * must list the deployed frontend origin in Cors__AllowedOrigins.
 */
const isLocal = typeof window !== 'undefined' &&
  (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1');

const API_ORIGIN = 'https://realestateai-api-p1nb.onrender.com';

export const API = isLocal ? 'http://localhost:5000/api' : `${API_ORIGIN}/api`;
export const UPLOADS = isLocal ? 'http://localhost:5000' : API_ORIGIN;

/** Resolve API-stored image paths against the API origin, leaving external URLs alone. */
export function assetUrl(path: string): string {
  if (!path) return '';
  if (/^(https?:|data:|blob:)/i.test(path)) return path;
  return `${UPLOADS}${path.startsWith('/') ? path : `/${path}`}`;
}
