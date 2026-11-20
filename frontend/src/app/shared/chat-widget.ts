import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AiService } from '../core/ai.service';

interface ChatMessage {
  from: 'ai' | 'user';
  text: string;
}

@Component({
  selector: 'app-chat-widget',
  imports: [FormsModule],
  templateUrl: './chat-widget.html',
  styleUrl: './chat-widget.scss'
})
export class ChatWidget {
  private ai = inject(AiService);

  open = signal(false);
  busy = signal(false);
  inputText = '';

  messages = signal<ChatMessage[]>([{
    from: 'ai',
    text: 'Assalam-o-Alaikum! Main aapka RealEstate AI agent hoon. Batayein aap kis city, budget aur type mein property chahte hain?'
  }]);

  suggestions = [
    '3 bed house in Lahore under 1 crore',
    'Plot in Bahria Town Karachi',
    'Apartment for rent in Islamabad',
    '5 marla house in DHA Lahore price?'
  ];

  toggle(): void {
    this.open.update(v => !v);
  }

  send(override?: string): void {
    const text = (override ?? this.inputText).trim();
    if (!text || this.busy()) return;

    this.messages.update(m => [...m, { from: 'user', text }]);
    this.inputText = '';
    this.busy.set(true);

    this.ai.chat(text).subscribe({
      next: res => this.messages.update(m => [...m, { from: 'ai', text: res.reply }]),
      error: () => this.messages.update(m => [...m, {
        from: 'ai', text: 'Sorry, abhi reply nahi mila. Thodi der baad dobara koshish karein.'
      }]),
      complete: () => this.busy.set(false)
    });
  }
}
