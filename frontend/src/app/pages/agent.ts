import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AgentService } from '../core/agent.service';
import { PropertyService } from '../core/property.service';
import { PropertyCard } from '../shared/property-card';
import { assetUrl } from '../core/api';
import { AgentProfile, PropertyDto } from '../core/models';

@Component({
  selector: 'app-agent',
  imports: [PropertyCard],
  templateUrl: './agent.html',
  styleUrl: './agent.scss'
})
export class AgentPage implements OnInit {
  private route = inject(ActivatedRoute);
  private agents = inject(AgentService);
  private properties = inject(PropertyService);

  profile = signal<AgentProfile | null>(null);
  listings = signal<PropertyDto[]>([]);
  loading = signal(true);
  error = signal('');
  assetUrl = assetUrl;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.agents.get(id).subscribe({
      next: p => { this.profile.set(p); this.loading.set(false); },
      error: () => { this.error.set('Agent profile not found.'); this.loading.set(false); }
    });
    this.properties.search({ agentId: id, pageSize: 48 }).subscribe(res => this.listings.set(res.items));
  }
}
