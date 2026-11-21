import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DisplayService } from '../core/display.service';
import { NEW_PROJECTS } from '../core/directory.data';
import { CITIES } from '../core/models';

@Component({
  selector: 'app-projects',
  imports: [FormsModule],
  templateUrl: './projects.html',
  styleUrl: './directory.scss'
})
export class ProjectsPage {
  display = inject(DisplayService);
  cities = CITIES;
  projects = NEW_PROJECTS;
  statuses = ['Launching', 'Under Construction', 'Nearing Completion'];

  city = signal('');
  status = signal('');

  filtered = computed(() => this.projects.filter(p =>
    (!this.city() || p.city === this.city()) &&
    (!this.status() || p.status === this.status())));
}
