namespace Clinica.Application.Common.Interfaces;

/// <summary>Mensagem de notificação para um destinatário da clínica.</summary>
public sealed record NotificacaoMensagem(
    Guid ClinicaId,
    Guid DestinatarioId,
    string? DestinatarioEmail,
    string Assunto,
    string Corpo);

/// <summary>
/// Envio de notificações (email hoje, SMS/push no futuro). A infraestrutura
/// concreta decide o canal; a aplicação depende apenas desta abstração.
/// </summary>
public interface INotificacaoService
{
    Task EnviarAsync(NotificacaoMensagem mensagem, CancellationToken ct);
}
