import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { AuthService } from './core/services/auth.service';
import { MockUserSummary } from './core/models/auth.model';

/** Shared password for every mock account - see backend MockUsers. Testing convenience only. */
const MOCK_PASSWORD = 'password123';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatCardModule,
    MatDividerModule
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly auth = inject(AuthService);
  protected readonly mockUsers = signal<MockUserSummary[]>([]);

  ngOnInit(): void {
    this.auth.listMockUsers().subscribe((users) => this.mockUsers.set(users));
  }

  selectUser(user: MockUserSummary): void {
    this.auth.login(user.username, MOCK_PASSWORD).subscribe();
  }

  logout(): void {
    this.auth.logout();
  }
}
