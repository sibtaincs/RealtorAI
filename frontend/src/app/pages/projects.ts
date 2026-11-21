import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContentService } from '../core/content.service';
import { DisplayService } from '../core/display.service';
import { NewProject } from '../core/directory.data';
import { CITIES } from '../core/models';

@Component({
  selector: 'app-projects',
  imports: [FormsModule],
  templateUrl: './projects.html',
  styleUrl: './directory.scss'
})
export class ProjectsPage implements OnInit {
  display = inject(DisplayService);
  private content = inject(ContentService);
  cities = CITIES;
  projects = signal<NewProject[]>([]);
  statuses = ['Launching', 'Under Construction', 'Nearing Completion'];

  city = signal('');
  status = signal('');

  ngOnInit(): void {
    this.content.list<NewProject>('project').subscribe(items => this.projects.set(items));
  }

  filtered = computed(() => this.projects().filter(p =>
    (!this.city() || p.city === this.city()) &&
    (!this.status() || p.status === this.status())));
}
