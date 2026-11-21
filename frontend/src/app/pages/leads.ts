import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LeadService } from '../core/lead.service';
import { FollowUpService } from '../core/follow-up.service';
import { AuthService } from '../core/auth.service';
import { FollowUp, LeadDto } from '../core/models';
import { MasterDataService } from '../core/master-data.service';

@Component({
  selector: 'app-leads',
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './leads.html',
  styleUrl: './leads.scss'
})
export class Leads implements OnInit {
  private leadService = inject(LeadService);
  private followUpService = inject(FollowUpService);
  auth = inject(AuthService);
  master = inject(MasterDataService);

  leads = signal<LeadDto[]>([]);
  status = '';
  loading = signal(true);
  error = signal('');

  expanded = signal<number | null>(null);
  followUps = signal<FollowUp[]>([]);
  loadingFollowUps = signal(false);
  generatedLink = signal('');

  // Phase 1: lead scoring + AI WhatsApp replies
  sortByScore = signal(false);
  generatingReply = signal(false);
  sortedLeads = computed(() => {
    const list = [...this.leads()];
    if (this.sortByScore()) list.sort((a, b) => b.score - a.score);
    return list;
  });

  ngOnInit(): void {
    if (this.auth.isAdmin()) this.load();
  }

  load(): void {
    this.loading.set(true);
    this.leadService.all(this.status || undefined).subscribe({
      next: res => {
        this.leads.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Leads load nahi hue.');
        this.loading.set(false);
      }
    });
  }

  changeStatus(lead: LeadDto, status: string): void {
    this.leadService.setStatus(lead.id, status).subscribe({
      next: updated => this.leads.update(list =>
        list.map(l => l.id === updated.id ? updated : l))
    });
  }

  toggleFollowUps(lead: LeadDto): void {
    if (this.expanded() === lead.id) {
      this.expanded.set(null);
      return;
    }
    this.expanded.set(lead.id);
    this.generatedLink.set('');
    this.loadingFollowUps.set(true);
    this.followUpService.forLead(lead.id).subscribe({
      next: items => {
        this.followUps.set(items);
        this.loadingFollowUps.set(false);
      },
      error: () => this.loadingFollowUps.set(false)
    });
  }

  createFollowUp(lead: LeadDto): void {
    this.followUpService.create(lead.id).subscribe({
      next: item => {
        this.followUps.update(list => [item, ...list]);
        this.expanded.set(lead.id);
        this.generatedLink.set(item.externalUrl ?? '');
      }
    });
  }

  /** Generates an AI WhatsApp reply to the lead's inquiry and saves it to history. */
  generateAiReply(lead: LeadDto): void {
    this.generatingReply.set(true);
    this.expanded.set(lead.id);
    this.followUpService.aiReply(lead.id, lead.message || 'Property available hai?').subscribe({
      next: item => {
        this.followUps.update(list => [item, ...list]);
        this.generatedLink.set(item.externalUrl ?? '');
        this.generatingReply.set(false);
      },
      error: () => {
        this.error.set('AI reply generate nahi hua. API check karein.');
        this.generatingReply.set(false);
      }
    });
  }

  whatsappLink(lead: LeadDto): string {
    let digits = (lead.phone ?? '').replace(/\D/g, '');
    if (digits.startsWith('00')) digits = digits.slice(2);
    if (digits.startsWith('0')) digits = '92' + digits.slice(1);
    else if (digits.length === 10 && digits.startsWith('3')) digits = '92' + digits;
    if (!digits.startsWith('92')) return '';

    const message = `Assalam-o-Alaikum ${lead.name}! Shukriya aapki inquiry ke liye. ` +
      `Main RealEstate AI agent hoon — ${lead.propertyTitle ?? 'our properties'} ke bare mein ` +
      `aapko similar options aur site visit ka time bhej sakta hoon.`;

    return `https://wa.me/${digits}?text=${encodeURIComponent(message)}`;
  }
}
