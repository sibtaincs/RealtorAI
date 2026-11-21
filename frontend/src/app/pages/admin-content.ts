import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import {
  CONTENT_FIELDS, CONTENT_KINDS, ContentField, ContentItem, ContentService, ContentUpsert
} from '../core/content.service';
import { MasterDataService } from '../core/master-data.service';

@Component({
  selector: 'app-admin-content',
  imports: [FormsModule, RouterLink],
  templateUrl: './admin-content.html',
  styleUrl: './admin-content.scss'
})
export class AdminContent implements OnInit {
  auth = inject(AuthService);
  private content = inject(ContentService);
  master = inject(MasterDataService);

  kinds = CONTENT_KINDS;
  items = signal<ContentItem[]>([]);
  kind = signal<string>(CONTENT_KINDS[0]);
  editingId = signal<number | null>(null);
  draft = signal<Record<string, any>>({});
  error = signal('');
  message = signal('');
  busy = signal(false);
  rawMode = signal(false);
  rawDraft = signal('{}');

  fields = computed(() => CONTENT_FIELDS[this.kind()] ?? []);
  filtered = computed(() => this.items().filter(item => item.kind === this.kind()));

  ngOnInit(): void {
    if (this.auth.isAdmin()) {
      this.master.load().subscribe();
      this.load();
    }
  }

  load(): void {
    this.content.all().subscribe({
      next: items => this.items.set(items),
      error: () => this.error.set('Content load nahi hua.')
    });
  }

  selectKind(kind: string): void {
    this.kind.set(kind);
    this.cancel();
  }

  defaultDraft(): Record<string, any> {
    const result: Record<string, any> = {};
    for (const field of this.fields()) {
      if (field.type === 'number') result[field.name] = 0;
      else if (field.type === 'boolean') result[field.name] = false;
      else result[field.name] = '';
    }
    return result;
  }

  edit(item: ContentItem): void {
    this.editingId.set(item.id);
    this.error.set('');
    this.message.set('');
    try {
      const parsed = JSON.parse(item.payloadJson);
      this.draft.set({ ...this.defaultDraft(), ...parsed });
      this.rawDraft.set(item.payloadJson);
    } catch {
      this.draft.set(this.defaultDraft());
      this.rawDraft.set('{}');
    }
  }

  cancel(): void {
    this.editingId.set(null);
    this.draft.set(this.defaultDraft());
    this.rawDraft.set('{}');
    this.error.set('');
    this.message.set('');
  }

  setField(name: string, value: any): void {
    this.draft.update(current => ({ ...current, [name]: value }));
  }

  cityOptions(): string[] {
    return this.master.values('city');
  }

  selectOptions(field: ContentField): string[] {
    if (field.type === 'city') return this.cityOptions();
    if (field.masterKind) return this.master.values(field.masterKind);
    return field.options ?? [];
  }

  save(): void {
    this.error.set('');
    this.message.set('');

    let payload: Record<string, any>;
    if (this.rawMode()) {
      try {
        payload = JSON.parse(this.rawDraft());
      } catch {
        this.error.set('JSON valid nahi hai.');
        return;
      }
      if (!payload || Array.isArray(payload) || typeof payload !== 'object') {
        this.error.set('JSON object hona chahiye.');
        return;
      }
    } else {
      payload = { ...this.draft() };
      for (const field of this.fields()) {
        if (field.required) {
          const value = payload[field.name];
          if (value === null || value === undefined || value === '') {
            this.error.set(`${field.label} required hai.`);
            return;
          }
        }
        if (field.type === 'number') {
          payload[field.name] = Number(payload[field.name]);
          if (Number.isNaN(payload[field.name])) payload[field.name] = 0;
        }
      }
    }

    this.busy.set(true);
    const input: ContentUpsert = { kind: this.kind(), payloadJson: JSON.stringify(payload) };
    const request = this.editingId()
      ? this.content.update(this.editingId()!, input)
      : this.content.create(input);
    request.subscribe({
      next: () => {
        this.busy.set(false);
        this.message.set('Content saved.');
        this.cancel();
        this.load();
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err?.error?.error || 'Save fail ho gaya.');
      }
    });
  }

  remove(id: number): void {
    this.busy.set(true);
    this.content.delete(this.kind(), id).subscribe({
      next: () => {
        this.busy.set(false);
        this.items.update(items => items.filter(item => item.id !== id));
        if (this.editingId() === id) this.cancel();
      },
      error: () => {
        this.busy.set(false);
        this.error.set('Delete fail ho gaya.');
      }
    });
  }
}