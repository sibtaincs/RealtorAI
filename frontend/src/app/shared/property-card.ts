import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PropertyDto, formatPkr } from '../core/models';
import { assetUrl } from '../core/api';

@Component({
  selector: 'app-property-card',
  imports: [RouterLink],
  templateUrl: './property-card.html',
  styleUrl: './property-card.scss'
})
export class PropertyCard {
  property = input.required<PropertyDto>();
  format = formatPkr;
  imageUrl = assetUrl;
}
