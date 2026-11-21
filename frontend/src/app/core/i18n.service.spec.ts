import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { I18nService } from './i18n.service';

describe('I18nService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
  });

  it('defaults to Roman Urdu and toggles to English', () => {
    const i18n = TestBed.inject(I18nService);

    expect(i18n.lang()).toBe('ur');
    expect(i18n.t('notif.empty')).toContain('Abhi');
    expect(document.documentElement.lang).toBe('ur-Latn');

    i18n.toggle();

    expect(i18n.lang()).toBe('en');
    expect(i18n.t('notif.empty')).toContain('no notifications');
    expect(localStorage.getItem('reai_lang')).toBe('en');
    expect(document.documentElement.lang).toBe('en');
  });
});
