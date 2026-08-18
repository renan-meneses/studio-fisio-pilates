import { defineConfig } from 'cypress';

export default defineConfig({
  e2e: {
    baseUrl: 'http://localhost:4200',
    supportFile: 'support/e2e.ts',
    specPattern: 'e2e/**/*.cy.ts',
    video: false,
    screenshotOnRunFailure: false,
  },
});