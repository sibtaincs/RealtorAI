import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { App } from './app';

describe('App shell', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    });
  });

  it('renders the navigation and switches the shared language', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('Post Property');
    expect(document.documentElement.lang).toBe('ur-Latn');

    const langButton = root.querySelector<HTMLButtonElement>('.lang');
    expect(langButton).not.toBeNull();
    langButton?.click();
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('en');
    expect(root.querySelector('.site-footer p')?.textContent)
      .toContain('Pakistani market');

    fixture.destroy();
  });
});
