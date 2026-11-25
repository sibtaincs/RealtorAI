import { Component, inject, input, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PropertyDto } from '../core/models';
import { assetUrl } from '../core/api';
import { DisplayService } from '../core/display.service';
import { ShortlistService } from '../core/shortlist.service';

@Component({
  selector: 'app-property-card',
  imports: [RouterLink],
  templateUrl: './property-card.html',
  styleUrl: './property-card.scss'
})
export class PropertyCard {
  property = input.required<PropertyDto>();
  display = inject(DisplayService);
  shortlist = inject(ShortlistService);
  imageUrl = assetUrl;

  compareCount = computed(() => this.shortlist.compare().length);
}