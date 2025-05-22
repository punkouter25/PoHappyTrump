using Microsoft.AspNetCore.Mvc;
using PoHappyTrump.Services;
using System.Threading.Tasks;

namespace PoHappyTrump.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TrumpMessageController : ControllerBase
    {
        private readonly TrumpMessageService _trumpMessageService;

        public TrumpMessageController(TrumpMessageService trumpMessageService)
        {
            _trumpMessageService = trumpMessageService;
        }

        /// <summary>
        /// Gets a random filtered message from the RSS feed.
        /// </summary>
        /// <returns>A random message string, or a 404 if no messages are found.</returns>
        [HttpGet]
        public async Task<ActionResult<string>> GetRandomMessage()
        {
            var message = await _trumpMessageService.GetRandomPositiveMessageAsync();

            if (string.IsNullOrEmpty(message))
            {
                return NotFound("No messages found with at least 10 words.");
            }

            return Ok(message);
        }
    }
}
