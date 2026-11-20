/**
 * Generates Netlify _redirects at build time from the API_ORIGIN environment
 * variable. Set API_ORIGIN in Netlify (Site configuration → Environment
 * variables) to your real Render service URL, e.g.
 * https://realestatepkai.onrender.com
 */
import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const origin = (process.env.API_ORIGIN || 'https://realestatepkai.onrender.com')
  .trim()
  .replace(/\/+$/, '');

const outDir = join(process.cwd(), 'dist', 'real-estate-ai');
mkdirSync(outDir, { recursive: true });

writeFileSync(
  join(outDir, '_redirects'),
  `/api/*       ${origin}/api/*      200\n` +
  `/uploads/*   ${origin}/uploads/*  200\n` +
  `/*           /index.html          200\n`,
  'utf8'
);

console.log(`[write-redirects] API origin: ${origin}`);
