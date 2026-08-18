/// <reference types="cypress" />
// Cypress e2e support file: comandos customizados e limpeza de estado.
beforeEach(() => {
  cy.clearLocalStorage();
});