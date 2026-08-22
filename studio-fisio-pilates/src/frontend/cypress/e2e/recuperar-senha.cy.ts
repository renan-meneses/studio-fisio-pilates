describe('Recuperação de senha', () => {
  beforeEach(() => {
    cy.intercept('GET', '**/api/**', { statusCode: 200, body: [] });
  });

  it('acessa a página pelo link "Esqueci minha senha" do login', () => {
    cy.visit('/login');
    cy.contains('a', 'Esqueci minha senha').click();
    cy.url().should('include', '/recuperar-senha');
    cy.contains('h1', 'Recuperar senha').should('be.visible');
  });

  it('confirma o envio com mensagem genérica (anti-enumeração)', () => {
    cy.intercept('POST', '**/auth/recuperar-senha', { statusCode: 204 }).as('solicitar');

    cy.visit('/recuperar-senha');
    cy.get('#email').type('alguem@demo.clinica');
    cy.contains('button', 'Enviar instruções').click();

    cy.wait('@solicitar');
    cy.contains('Se o e-mail estiver cadastrado').should('be.visible');
    // Botão vira "Reenviar" sem revelar se o e-mail existe.
    cy.contains('button', 'Reenviar').should('be.visible');
  });

  it('redefine a senha com token válido e volta ao login', () => {
    cy.intercept('POST', '**/auth/redefinir-senha', { statusCode: 204 }).as('redefinir');

    cy.visit('/redefinir-senha?email=alguem%40demo.clinica&token=abc123');
    cy.get('#senha').type('NovaSenha@1');
    cy.contains('button', 'Redefinir senha').click();

    cy.wait('@redefinir')
      .its('request.body')
      .should(body => {
        expect(body.email).to.eq('alguem@demo.clinica');
        expect(body.token).to.eq('abc123');
        expect(body.novaSenha).to.eq('NovaSenha@1');
      });
    cy.url().should('include', '/login');
  });

  it('avisa quando o link não tem token/e-mail válidos', () => {
    cy.visit('/redefinir-senha');
    cy.contains('Link inválido').should('be.visible');
    cy.contains('a', 'Solicitar novo token').should('be.visible');
  });
});
