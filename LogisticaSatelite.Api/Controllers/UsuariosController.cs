using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticaSatelite.Api.Data;
using LogisticaSatelite.Api.Models;

namespace LogisticaSatelite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<ActionResult<Usuario>> Login([FromBody] LoginDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.User) || string.IsNullOrWhiteSpace(dto.Senha))
                return BadRequest(new { message = "Matrícula/E-mail e senha são obrigatórios." });

            // Valida na tabela Usuarios do SQLite (Case Insensitive)
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.User.ToLower() == dto.User.Trim().ToLower()
                                       && u.Senha == dto.Senha.Trim());

            if (usuario == null)
                return Unauthorized(new { message = "Usuário ou senha inválidos." });

            return Ok(usuario);
        }
    }

    public class LoginDTO
    {
        public string User { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }
}