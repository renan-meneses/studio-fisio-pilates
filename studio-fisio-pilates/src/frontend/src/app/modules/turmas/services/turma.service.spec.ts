import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { TurmaService } from './turma.service';

describe('TurmaService — waitlist', () => {
  let service: TurmaService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TurmaService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('entrar na fila envia pacienteId para o endpoint da turma', () => {
    const turmaId = '11111111-1111-1111-1111-111111111111';
    const pacienteId = '22222222-2222-2222-2222-222222222222';
    const entradaId = '33333333-3333-3333-3333-333333333333';

    service.entrarWaitlist(turmaId, pacienteId).subscribe(resposta => {
      expect(resposta.id).toBe(entradaId);
    });

    const req = http.expectOne(`${environment.apiUrl}/turmas/${turmaId}/waitlist`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ pacienteId });
    req.flush({ id: entradaId });
  });

  it('listar fila consulta o endpoint correto', () => {
    const turmaId = '11111111-1111-1111-1111-111111111111';

    service.waitlist(turmaId).subscribe(entradas => {
      expect(entradas.length).toBe(1);
      expect(entradas[0].pacienteNome).toBe('Aluno Teste');
    });

    const req = http.expectOne(`${environment.apiUrl}/turmas/${turmaId}/waitlist`);
    expect(req.request.method).toBe('GET');
    req.flush([
      { id: 'e1', pacienteId: 'p1', pacienteNome: 'Aluno Teste', entradaEm: '2026-08-21T12:00:00Z' },
    ]);
  });

  it('sair da fila usa DELETE com id da entrada', () => {
    const turmaId = '11111111-1111-1111-1111-111111111111';
    const entradaId = '33333333-3333-3333-3333-333333333333';

    service.sairWaitlist(turmaId, entradaId).subscribe(resposta => {
      expect(resposta).toBeNull();
    });

    const req = http.expectOne(
      `${environment.apiUrl}/turmas/${turmaId}/waitlist/${entradaId}`,
    );
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
