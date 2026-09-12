using LogisticaSatelite.Api.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

public class GlobalExceptionHandler : IExceptionHandler
// marca a classe como um manipulador de exceções global, implementando a interface IExceptionHandler para fornecer uma maneira de lidar com exceções não tratadas em toda a aplicação.
{
    public async ValueTask<bool> TryHandleAsync(// Define o método TryHandleAsync que tenta lidar com exceções não tratadas.
        HttpContext httpContext,// Recebe o contexto HTTP da solicitação atual, permitindo acesso a informações sobre a solicitação e a resposta.
        Exception exception,// Recebe a exceção que ocorreu durante o processamento da solicitação, permitindo que o método determine como lidar com ela.
        CancellationToken cancellationToken)// Recebe um token de cancelamento que pode ser usado para cancelar a operação assíncrona, caso necessário.
    {
        // Define o código de status HTTP padrão para falhas inesperadas
        var statusCode = StatusCodes.Status500InternalServerError;// Define o código de status HTTP padrão para falhas inesperadas
        var title = "Erro interno no servidor";// Define o título padrão para falhas inesperadas
        var detail = exception.Message;// Define o detalhe padrão para falhas inesperadas, usando a mensagem da exceção

        // Personalizações pontuais baseadas no tipo de exceção
        if (exception is KeyNotFoundException)
           // Verifica se a exceção é do tipo KeyNotFoundException, que indica que um recurso não foi encontrado.
        {
            statusCode = StatusCodes.Status404NotFound;// Altera o código de status HTTP para 404 Not Found, indicando que o recurso solicitado não foi encontrado.
            title = "Recurso não encontrado";
        }

        if (exception is ExceptionTemplate)
        {
            statusCode = StatusCodes.Status422UnprocessableEntity;
            title = "Regra de negócio violada";
        }

        var problemDetails = new ProblemDetails // Cria um objeto ProblemDetails para fornecer informações detalhadas sobre o erro ocorrido, incluindo o código de status, título, detalhes e a instância da solicitação.
        {
            Status = statusCode, // Define o código de status HTTP no objeto ProblemDetails.
            Title = title,// Define o título do erro no objeto ProblemDetails.
            Detail = detail,// Define o detalhe do erro no objeto ProblemDetails.
            Instance = httpContext.Request.Path// Define a instância da solicitação no objeto ProblemDetails, usando o caminho da solicitação atual.
        };

        httpContext.Response.StatusCode = statusCode; // Define o código de status HTTP na resposta, garantindo que o cliente receba o status correto.
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);// Serializa o objeto ProblemDetails em JSON e escreve na resposta HTTP, permitindo que o cliente receba informações detalhadas sobre o erro ocorrido.

        // Retorna true sinalizando que a exceção foi processada com segurança
        return true;
    }
}
