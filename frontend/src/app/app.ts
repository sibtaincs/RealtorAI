import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { NotificationService } from './core/notification.service';
import { NotificationItem } from './core/models';
import { ChatWidget } from './shared/chat-widget';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ChatWidget],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  auth = inject(AuthService);
  notifications = inject(NotificationService);
  private router = inject(Router);

  notifOpen = signal(false);

  constructor() {
    // Keep the bell count in sync when the user logs in/out without a full reload.
    effect(() => {
      if (this.auth.isLoggedIn()) this.notifications.refreshCount();
      else this.notifications.unread.set(0);
    });
  }

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) this.notifications.refreshCount();
  }

  toggleNotifications(): void {
    this.notifOpen.update(v => !v);
    if (this.notifOpen() && this.auth.isLoggedIn()) {
      this.notifications.all().subscribe();
    }
  }

  openNotification(item: NotificationItem): void {
    this.notifications.markRead(item.id).subscribe();
    this.notifOpen.set(false);
    if (item.link) this.router.navigateByUrl(item.link);
  }

  markAllRead(): void {
    this.notifications.markAll().subscribe();
  }

  logout(): void {
    this.notifOpen.set(false);
    this.notifications.unread.set(0);
    this.auth.logout();
  }
}
