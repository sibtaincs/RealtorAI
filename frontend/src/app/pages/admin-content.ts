import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { CONTENT_KINDS, ContentItem, ContentService } from '../core/content.service';

@Component({
  selector: 'app-admin-content',
  imports: [FormsModule, RouterLink],
  templateUrl: './admin-content.html',
  styleUrl: './admin-content.scss'
})
export class AdminContent implements OnInit {
  auth = inject(AuthService);
  private content = inject(ContentService);

  kinds = CONTENT_KINDS;
  items = signal<ContentItem[]>([]);
  kind = signal<string>(CONTENT_KINDS[0]);
  editingId = signal<number | null>(null);
  draft = signal('{}');
  error = signal('');
  message = signal('');
  busy = signal(false);

  ngOnInit(): void {
    if (this.auth.isAdmin()) this.load();
  }

  filtered = computed(() => this.items().filter(item => item.kind === this.kind()));

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

  edit(item: ContentItem): void {
    this.editingId.set(item.id);
    this.draft.set(item.payloadJson);
    this.error.set('');
    this.message.set('');
  }

  cancel(): void {
    this.editingId.set(null);
    this.draft.set('{}');
  }

  save(): void {
    this.error.set('');
    this.message.set('');
    let parsed: unknown;
    try {
      parsed = JSON.parse(this.draft());
    } catch {
      this.error.set('JSON valid nahi hai.');
      return;
    }
    if (!parsed || Array.isArray(parsed) || typeof parsed !== 'object') {
      this.error.set('JSON object hona chahiye.');
      return;
    }

    this.busy.set(true);
    const input = { kind: this.kind(), payloadJson: JSON.stringify(parsed) };
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
      error: () => {
        this.busy.set(false);
        this.error.set('Save fail ho gaya.');
      }
    });
  }

  remove(id: number): void {
    this.busy.set(true);
    this.content.delete(id).subscribe({
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
