import { Injectable } from '@angular/core';

/**
 * Single entry point for printing official documents.
 * While printing, <body> gets `printing-doc`, which hides `.screen-only`
 * elements and shows the `.print-only` document sheet (see styles.css).
 */
@Injectable({ providedIn: 'root' })
export class PrintService {
  print(): void {
    const body = document.body;
    body.classList.add('printing-doc');

    const cleanup = () => {
      body.classList.remove('printing-doc');
      window.removeEventListener('afterprint', cleanup);
    };
    window.addEventListener('afterprint', cleanup);

    // Give Angular a tick to render any print-only content that depends on component state
    setTimeout(() => window.print(), 60);
  }
}
