namespace Clinica.Domain.Enums;

public enum TipoPessoa
{
    Fisica = 1,
    Juridica = 2,
}

public enum Sexo
{
    Feminino = 1,
    Masculino = 2,
    Outro = 3,
}

public enum StatusPaciente
{
    Ativo = 1,
    Inativo = 2,
    Suspenso = 3,
}

public enum StatusAgendamento
{
    Agendado = 1,
    Confirmado = 2,
    Realizado = 3,
    Cancelado = 4,
    Faltou = 5,
}

public enum TipoSessao
{
    Avaliacao = 1,
    PilatesSolo = 2,
    PilatesDupla = 3,
    PilatesGrupo = 4,
    Fisioterapia = 5,
    Domiciliar = 6,
}

public enum StatusPresenca
{
    Presente = 1,
    Ausente = 2,
    EmAtraso = 3,
}

public enum TipoEvolucao
{
    Anamnese = 1,
    Evolucao = 2,
    Relatorio = 3,
    Alta = 4,
}

public enum StatusMensalidade
{
    Pendente = 1,
    Paga = 2,
    Atrasada = 3,
    Cancelada = 4,
}

public enum StatusContaPagar
{
    EmAberto = 1,
    Paga = 2,
    Vencida = 3,
}

public enum TipoCusto
{
    Fixo = 1,
    Variavel = 2,
    Pessoal = 3,
}

public enum StatusFolha
{
    Rascunho = 1,
    Processada = 2,
    Paga = 3,
    Cancelada = 4,
}