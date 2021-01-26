using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CircuitBreakerRetry.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LivrosController : ControllerBase
    {
        static int _requestCount;
        static int _requestERROCount;

        private readonly ILogger<LivrosController> _logger;

        private static readonly Livro[] Livros = new Livro[]
        {
            new Livro{ Id=1, Nome="Código limpo"},
            new Livro{ Id=2, Nome="DDD"},
            new Livro{ Id=3, Nome="Quebrando a cabeça com C#"},
            new Livro{ Id=4, Nome="Scrum Master"},
        };

        public LivrosController(ILogger<LivrosController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            Console.WriteLine($"Request {_requestCount}");
            Console.WriteLine($"Erro {_requestERROCount}");

            _requestCount++;

            if (_requestCount % 5 == 0)
            {
                _requestCount--;

                _requestERROCount++;

                if (_requestERROCount == 4)
                {
                    _requestCount++;
                    _requestERROCount = 0;
                }

                return BadRequest();
            }
            else
            {
                return Ok(Livros);
            }
        }

        [HttpGet("iid")]
        public async Task<IActionResult> Get(int iid)
        {
            List<Livro> lista = new List<Livro>();

            return Ok(Livros.Where(id => id.Id == iid));
        }
    }
}
