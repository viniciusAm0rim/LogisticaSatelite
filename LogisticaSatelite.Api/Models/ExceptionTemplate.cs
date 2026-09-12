
using System.Security.Cryptography.X509Certificates;

namespace LogisticaSatelite.Api.Models
{
    public class ExceptionTemplate : Exception
    // Classe para padronizar o tratamento de exceções na aplicação, herdando da classe base Exception para fornecer funcionalidades de exceção personalizadas.
    {
        public ExceptionTemplate(string message) : base(message)
        // Construtor da classe ExceptionTemplate que recebe uma mensagem de erro como parâmetro e a passa para o construtor da classe base Exception, permitindo que a exceção personalizada seja criada com uma mensagem específica.
        {
          

        }

    }
}
