import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { NotificationService } from './core/notification.service';
import { NotificationItem } from './core/models';
import { I18nService } from './core/i18n.service';
import { TranslationKey } from './core/i18n.ur';
import { ChatWidget } from './shared/chat-widget';
import { AREA_UNITS, CURRENCIES, DisplayService } from './core/display.service';

interface InstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ChatWidget],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  auth = inject(AuthService);
  notifications = inject(NotificationService);
  i18n = inject(I18nService);
  display = inject(DisplayService);
  currencies = CURRENCIES;
  areaUnits = AREA_UNITS;
  private router = inject(Router);

  notifOpen = signal(false);
  installEvent = signal<InstallPromptEvent | null>(null);
  t = (key: TranslationKey) => this.i18n.t(key);

  constructor() {
    // Keep the bell count in sync when the user logs in/out without a full reload.
    effect(() => {
      if (this.auth.isLoggedIn()) this.notifications.refreshCount();
      else this.notifications.unread.set(0);
    });

    if (typeof window !== 'undefined') {
      window.addEventListener('beforeinstallprompt', (event) => {
        event.preventDefault();
        this.installEvent.set(event as InstallPromptEvent);
      });
    }
  }

  async install(): Promise<void> {
    const event = this.installEvent();
    if (!event) return;
    await event.prompt();
    await event.userChoice;
    this.installEvent.set(null);
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
