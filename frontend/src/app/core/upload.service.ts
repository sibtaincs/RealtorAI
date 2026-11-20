import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UploadResult } from './models';
import { API } from './api';

@Injectable({ providedIn: 'root' })
export class UploadService {
  private http = inject(HttpClient);

  upload(files: File[]): Observable<UploadResult[]> {
    const body = new FormData();
    files.forEach(file => body.append('files', file, file.name));
    return this.http.post<UploadResult[]>(`${API}/uploads/multiple`, body);
  }
}
