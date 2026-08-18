describe('Login (smoke)', () => {
  beforeEach(() => {
    cy.visit('/');
  });

  it('redireciona para a página de login sem sessão', () => {
    cy.url().should('include', '/login');
  });

  it('exibe os campos de credenciais', () => {
    cy.get('input#email').should('be.visible');
    cy.get('input#senha').should('be.visible');
  });
});