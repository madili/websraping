using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebScraping.Models;
using WebScraping.Services;

namespace WebScraping.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScrapingController : ControllerBase
    {
        [Route("/")]
        [HttpGet]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Swagger()
        {
            return Redirect("/swagger");
        }

        /// <summary>
        /// Listar Modelo de Aparelhos
        /// </summary>
        /// <response code="200">Lista de Aparelhos</response>
        /// <response code="400">
        /// </response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ViewModel>), 200)]
        [ProducesResponseType(400)]
        [SwaggerOperation(OperationId = "ScrapingGetAsync")]
        public async Task<ActionResult<IEnumerable<ViewModel>>> GetAsync(
            [FromQuery] string user,
            [FromQuery] string repo,
            [FromServices] ScrapingService scrapingService)
        {
            try
            {
                var endpoint = $"{user}/{repo}";

                return (await scrapingService.GetResultRepositoryAsync(endpoint)).ToList();
            }
            catch (Exception)
            {
                return BadRequest("It was not possible to obtain results for this repository");
            }
        }
    }
}
