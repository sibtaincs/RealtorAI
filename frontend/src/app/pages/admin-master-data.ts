import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import {
  MASTER_DATA_KINDS, MasterDataItem, MasterDataService, MasterDataUpsert
} from '../core/master-data.service';

@Component({
  selector: 'app-admin-master-data',
  imports: [FormsModule, RouterLink],
  templateUrl: './admin-master-data.html',
  styleUrl: './admin-content.scss'
})
export class AdminMasterData implements OnInit {
  auth = inject(AuthService);
  master = inject(MasterDataService);

  kinds = MASTER_DATA_KINDS;
  items = signal<MasterDataItem[]>([]);
  kind = signal<string>(MASTER_DATA_KINDS[0]);
  editingId = signal<number | null>(null);
  draft: MasterDataUpsert = this.blankDraft(MASTER_DATA_KINDS[0]);
  error = signal('');
  message = signal('');
  busy = signal(false);

  filtered = computed(() => this.items()
    .filter(item => item.kind === this.kind())
    .sort((a, b) => a.sortOrder - b.sortOrder || a.id - b.id));

  ngOnInit(): void {
    if (this.auth.isAdmin()) this.load();
  }

  load(): void {
    this.master.load().subscribe({
      next: items => this.items.set(items),
      error: () => this.error.set('Master data load nahi hua.')
    });
  }

  selectKind(kind: string): void {
    this.kind.set(kind);
    this.cancel();
  }

  startAdd(): void {
    this.draft = this.blankDraft(this.kind(), this.filtered().length + 1);
    this.editingId.set(null);
    this.error.set('');
    this.message.set('');
  }

  edit(item: MasterDataItem): void {
    this.editingId.set(item.id);
    this.draft = {
      kind: item.kind,
      value: item.value,
      label: item.label,
      sortOrder: item.sortOrder,
      isActive: item.isActive
    };
    this.error.set('');
    this.message.set('');
  }

  cancel(): void {
    this.editingId.set(null);
    this.draft = this.blankDraft(this.kind());
  }

  save(): void {
    if (!this.draft.value.trim()) {
      this.error.set('Value required hai.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    const request = this.editingId()
      ? this.update(this.editingId()!)
      : this.master.create(this.draft);
    request.subscribe({
      next: () => {
        this.busy.set(false);
        this.message.set('Master data saved.');
        this.items.set([]);
        this.master.reload().subscribe(items => {
          this.items.set(items);
          this.cancel();
        });
      },
      error: () => {
        this.busy.set(false);
        this.error.set('Save fail ho gaya. Fixed values ka label hi badla ja sakta hai.');
      }
    });
  }

  remove(id: number): void {
    this.busy.set(true);
    this.master.delete(id).subscribe({
      next: () => {
        this.busy.set(false);
        this.items.update(items => items.filter(item => item.id !== id));
        if (this.editingId() === id) this.cancel();
      },
      error: () => {
        this.busy.set(false);
        this.error.set('Delete fail ho gaya. Fixed values deactivate karein.');
      }
    });
  }

  private update(id: number) {
    return this.master.update(id, this.draft);
  }

  private blankDraft(kind: string, sortOrder = 0): MasterDataUpsert {
    return { kind, value: '', label: '', sortOrder, isActive: true };
  }
}
