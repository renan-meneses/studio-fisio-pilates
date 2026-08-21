using Clinica.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Clinica.CrossCutting.Notifications;

/// <summary>
/// Stub de notificação: registra a mensagem em log estruturado. Ponto único
/// de troca por um provedor real (SMTP, SES, SendGrid...) sem tocar na
/// aplicação — basta registrar outra implementação de INotificacaoService.
/// </summary>
public sealed class LoggingNotificacaoService : INotificacaoService
{
    private readonly ILogger<LoggingNotificacaoService> _logger;

    public LoggingNotificacaoService(ILogger<LoggingNotificacaoService> logger)
    {
        _logger = logger;
    }

    public Task EnviarAsync(NotificacaoMensagem mensagem, CancellationToken ct)
    {
        _logger.LogInformation(
            "NOTIFICACAO clinica={ClinicaId} destinatario={DestinatarioId} email={Email} assunto={Assunto} corpo={Corpo}",
            mensagem.ClinicaId,
            mensagem.DestinatarioId,
            mensagem.DestinatarioEmail ?? "<sem-email>",
            mensagem.Assunto,
            mensagem.Corpo);

        return Task.CompletedTask;
    }
}
