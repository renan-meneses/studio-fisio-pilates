import { Injectable, signal } from '@angular/core';
import { SessionStore } from '../models/session.model';

export type Tema = 'Claro' | 'Escuro';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly tema = signal<Tema>(this.inicial());

  constructor() {
    this.aplicar(this.tema());
  }

  /** Alterna entre claro/escuro, aplica no DOM e persiste por usuário. */
  alternar(): Tema {
    const novo = this.tema() === 'Claro' ? 'Escuro' : 'Claro';
    this.tema.set(novo);
    this.aplicar(novo);
    this.persistir(novo);
    return novo;
  }

  aplicar(tema: Tema): void {
    document.documentElement.setAttribute('data-theme', tema === 'Escuro' ? 'dark' : 'light');
  }

  sincronizar(tema: Tema): void {
    this.tema.set(tema);
    this.aplicar(tema);
  }

  private persistir(tema: Tema): void {
    const usuario = SessionStore.userName();
    if (usuario) {
      localStorage.setItem(`clinica.tema.${usuario}`, tema);
    }
  }

  private inicial(): Tema {
    const doUsuario = SessionStore.userTema();
    if (doUsuario) {
      return doUsuario;
    }
    const usuario = SessionStore.userName();
    const local = usuario ? localStorage.getItem(`clinica.tema.${usuario}`) : null;
    if (local === 'Escuro' || local === 'Claro') {
      return local;
    }
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'Escuro' : 'Claro';
  }
}
