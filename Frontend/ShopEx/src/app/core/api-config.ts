import { isDevMode } from '@angular/core';

/**
 * Dev: the API runs separately on https://localhost:7122.
 * Production: the SPA is served by the API itself, so a same-origin relative path is used.
 */
export const API_URL = isDevMode() ? 'https://localhost:7122/api' : '/api';
