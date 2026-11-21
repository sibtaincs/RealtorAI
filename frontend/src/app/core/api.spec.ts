import { describe, expect, it } from 'vitest';
import { assetUrl } from './api';

describe('assetUrl', () => {
  it('returns an empty string for a missing path', () => {
    expect(assetUrl('')).toBe('');
  });

  it('leaves absolute URLs alone', () => {
    expect(assetUrl('https://example.com/a.jpg')).toBe('https://example.com/a.jpg');
    expect(assetUrl('data:image/png;base64,abc')).toBe('data:image/png;base64,abc');
  });

  it('resolves stored paths against the uploads origin', () => {
    expect(assetUrl('/uploads/a.jpg')).toBe('http://localhost:5000/uploads/a.jpg');
    expect(assetUrl('uploads/a.jpg')).toBe('http://localhost:5000/uploads/a.jpg');
  });
});
