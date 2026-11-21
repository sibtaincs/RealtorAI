import { Component, ElementRef, HostListener, OnInit, ViewChild, effect, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { NotificationService } from './core/notification.service';
import { NotificationItem } from './core/models';
import { I18nService } from './core/i18n.service';
import { TranslationKey } from './core/i18n.ur';
import { ChatWidget } from './shared/chat-widget';
import { DisplayService } from './core/display.service';
import { MasterDataService } from './core/master-data.service';

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
  master = inject(MasterDataService);
  private router = inject(Router);

  @ViewChild('tools') private toolsMenu?: ElementRef<HTMLDetailsElement>;
  @ViewChild('explore') private exploreMenu?: ElementRef<HTMLDetailsElement>;
  @ViewChild('settings') private settingsMenu?: ElementRef<HTMLDetailsElement>;

  notifOpen = signal(false);
  mobileMenuOpen = signal(false);
  installEvent = signal<InstallPromptEvent | null>(null);
  t = (key: TranslationKey) => this.i18n.t(key);

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!(event.target as HTMLElement).closest('.nav-menu, .notif-wrap')) this.closeMenus();
    if (!(event.target as HTMLElement).closest('.links, .hamburger')) this.mobileMenuOpen.set(false);
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') this.closeMenus();
  }

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
    this.master.load().subscribe();
    if (this.auth.isLoggedIn()) this.notifications.refreshCount();
  }

  closeMenus(except?: 'tools' | 'explore' | 'settings' | 'notif'): void {
    if (except !== 'tools' && this.toolsMenu) this.toolsMenu.nativeElement.open = false;
    if (except !== 'explore' && this.exploreMenu) this.exploreMenu.nativeElement.open = false;
    if (except !== 'settings' && this.settingsMenu) this.settingsMenu.nativeElement.open = false;
    if (except !== 'notif') this.notifOpen.set(false);
  }

  onMenuToggle(menu: 'tools' | 'explore' | 'settings', open: boolean): void {
    if (open) this.closeMenus(menu);
  }

  toggleNotifications(): void {
    this.closeMenus('notif');
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
