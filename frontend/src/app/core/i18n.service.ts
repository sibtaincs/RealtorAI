import { Injectable, signal } from '@angular/core';
import { en } from './i18n.en';
import { TranslationKey, ur } from './i18n.ur';

type Language = 'ur' | 'en';

@Injectable({ providedIn: 'root' })
export class I18nService {
  private stored = typeof localStorage !== 'undefined' ? localStorage.getItem('reai_lang') : null;
  lang = signal<Language>(this.stored === 'en' ? 'en' : 'ur');

  constructor() {
    this.apply();
  }

  t(key: TranslationKey): string {
    return this.lang() === 'en' ? en[key] : ur[key];
  }

  toggle(): void {
    const next: Language = this.lang() === 'en' ? 'ur' : 'en';
    this.lang.set(next);
    try {
      localStorage.setItem('reai_lang', next);
    } catch {
      // Storage may be disabled; the in-memory choice still applies for this session.
    }
    this.apply();
  }

  private apply(): void {
    if (typeof document !== 'undefined') {
      document.documentElement.lang = this.lang() === 'en' ? 'en' : 'ur-Latn';
    }
  }
}
