import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppUser, CreateUserRequest } from '../models/models';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly baseUrl = `${AuthService.apiUrl}/Users`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<AppUser[]> {
    return this.http.get<AppUser[]>(this.baseUrl);
  }

  create(request: CreateUserRequest): Observable<AppUser> {
    return this.http.post<AppUser>(this.baseUrl, {
      name: request.name.trim(),
      phone: request.phone.trim(),
      role: Number(request.role),
      username: request.username.trim(),
      password: request.password,
      storeId: request.storeId ? Number(request.storeId) : null
    });
  }

  updatePassword(id: number, newPassword: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/password`, { newPassword });
  }

  setActive(id: number, active: boolean): Observable<AppUser> {
    return this.http.post<AppUser>(`${this.baseUrl}/${id}/${active ? 'enable' : 'disable'}`, {});
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
