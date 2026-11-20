import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DocumentService } from '../core/document.service';
import { AuthService } from '../core/auth.service';
import { DocumentItem } from '../core/models';

@Component({
  selector: 'app-document-checklist',
  imports: [FormsModule, DatePipe, RouterLink],
  templateUrl: './document-checklist.html',
  styleUrl: './document-checklist.scss'
})
export class DocumentChecklist implements OnInit {
  propertyId = input.required<number>();

  private documents = inject(DocumentService);
  auth = inject(AuthService);

  items = signal<DocumentItem[]>([]);
  loading = signal(true);
  busy = signal(false);
  error = signal('');
  newName = '';

  verifiedCount = computed(() => this.items().filter(i => i.isVerified).length);
  progress = computed(() =>
    this.items().length === 0 ? 0 : Math.round(this.verifiedCount() / this.items().length * 100));

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.documents.forProperty(this.propertyId()).subscribe({
      next: items => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Checklist load nahi hua.');
        this.loading.set(false);
      }
    });
  }

  toggle(item: DocumentItem): void {
    if (!this.auth.isLoggedIn()) {
      this.error.set('Document verify karne ke liye agent login zaroori hai.');
      return;
    }
    this.error.set('');
    this.busy.set(true);
    this.documents.update(item.id, !item.isVerified, item.notes).subscribe({
      next: updated => {
        this.items.update(list => list.map(i => i.id === updated.id ? updated : i));
        this.busy.set(false);
      },
      error: () => {
        this.error.set('Update nahi hua. Dobara try karein.');
        this.busy.set(false);
      }
    });
  }

  add(): void {
    if (!this.auth.isLoggedIn()) {
      this.error.set('Item add karne ke liye agent login zaroori hai.');
      return;
    }
    if (!this.newName.trim()) return;

    this.error.set('');
    this.busy.set(true);
    this.documents.add(this.propertyId(), this.newName.trim()).subscribe({
      next: item => {
        this.items.update(list => [...list, item]);
        this.newName = '';
        this.busy.set(false);
      },
      error: () => {
        this.error.set('Item add nahi hua.');
        this.busy.set(false);
      }
    });
  }

  remove(item: DocumentItem): void {
    if (!this.auth.isLoggedIn()) return;
    this.documents.remove(item.id).subscribe({
      next: () => this.items.update(list => list.filter(i => i.id !== item.id))
    });
  }
}
