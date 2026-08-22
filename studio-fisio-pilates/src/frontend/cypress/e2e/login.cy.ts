describe('Autenticação', () => {
  beforeEach(() => {
    // Silencia as chamadas do shell pós-login; o foco aqui é o fluxo de auth.
    cy.intercept('GET', '**/api/**', { statusCode: 200, body: [] });
    cy.visit('/login');
  });

  it('exibe o formulário e valida campos obrigatórios', () => {
    cy.contains('h1', 'Entrar').should('be.visible');
    cy.get('#email').should('be.visible');
    cy.get('#senha').should('be.visible');

    cy.contains('button', 'Entrar').should('be.disabled');
    cy.get('#email').type('admin@demo.clinica');
    cy.contains('button', 'Entrar').should('be.disabled'); // falta a senha
  });

  it('faz login com credenciais válidas e vai para a agenda', () => {
    cy.intercept('POST', '**/auth/login', { fixture: 'login-ok.json' }).as('login');

    cy.get('#email').type('admin@demo.clinica');
    cy.get('#senha').type('Admin@Demo123');
    cy.contains('button', 'Entrar').click();

    cy.wait('@login').its('response.statusCode').should('eq', 200);
    cy.url().should('include', '/agenda');
    // Sessão persistida para o authGuard.
    cy.window()
      .then(w => w.localStorage.getItem('clinica.access_token'))
      .should('eq', 'e2e.access.token');
  });

  it('exibe mensagem de erro com credenciais inválidas', () => {
    cy.intercept('POST', '**/auth/login', { statusCode: 401, body: { title: 'Credenciais inválidas.' } }).as('login');

    cy.get('#email').type('admin@demo.clinica');
    cy.get('#senha').type('errada');
    cy.contains('button', 'Entrar').click();

    cy.wait('@login');
    cy.contains('.login-card__error', 'Credenciais inválidas.').should('be.visible');
    cy.url().should('include', '/login');
  });
});
