import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DocumentService } from '../core/document.service';
import { AuthService } from '../core/auth.service';
import { DocumentItem, OcrScanResult } from '../core/models';

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

  // Phase 2: OCR document scanning
  scanning = signal(false);
  scanNotice = signal('');
  scanResult = signal<OcrScanResult | null>(null);
  scanText = '';

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

  /** Reads a document photo in the browser with Tesseract OCR, then auto-verifies items. */
  async scanFile(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    if (!this.auth.isLoggedIn()) {
      this.error.set('Scan ke liye agent login zaroori hai.');
      return;
    }

    this.scanning.set(true);
    this.scanNotice.set('OCR model load ho raha hai... pehli baar thoda time lag sakta hai.');
    this.scanResult.set(null);

    try {
      const { createWorker } = await import('tesseract.js');
      const worker = await createWorker('eng');
      this.scanNotice.set('Document padha ja raha hai...');
      const { data } = await worker.recognize(file);
      await worker.terminate();
      this.sendForVerification(data.text);
    } catch {
      this.scanning.set(false);
      this.scanNotice.set('OCR available nahi hai. Neeche text paste karke verify karein.');
    }
  }

  /** Manual fallback when OCR cannot run (e.g. offline on first load). */
  scanFromText(): void {
    if (!this.auth.isLoggedIn()) {
      this.error.set('Scan ke liye agent login zaroori hai.');
      return;
    }
    if (!this.scanText.trim()) return;
    this.sendForVerification(this.scanText);
  }

  private sendForVerification(text: string): void {
    this.scanning.set(true);
    this.documents.scan(this.propertyId(), text).subscribe({
      next: result => {
        this.items.set(result.items);
        this.scanResult.set(result);
        this.scanText = '';
        this.scanning.set(false);
        this.scanNotice.set(result.newlyVerified > 0
          ? `✅ ${result.newlyVerified} documents auto-verified ho gaye.`
          : result.matched > 0
            ? 'Document pehle se verified tha.'
            : 'Is text se koi checklist item match nahi hua.');
      },
      error: () => {
        this.scanning.set(false);
        this.scanNotice.set('Scan save nahi hua. Dobara try karein.');
      }
    });
  }
}
