using Microsoft.AspNetCore.Mvc;
using PoHappyTrump.Services;
using PoHappyTrump.Models;
using System.Threading.Tasks;

namespace PoHappyTrump.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        /// <summary>
        /// Gets a random original message from the RSS feed without any transformation.
        /// </summary>
        /// <returns>A random original message string, or a 404 if no messages are found.</returns>
        [HttpGet("original")]
        public async Task<ActionResult<string>> GetRandomOriginalMessage()
        {
            var message = await _trumpMessageService.GetRandomOriginalMessageAsync();

            if (string.IsNullOrEmpty(message))
            {
                return NotFound("No messages found with at least 1 word.");
            }

            return Ok(message);
        }

        /// <summary>
        /// Gets both the original and enhanced version of a random message for comparison.
        /// </summary>
        /// <returns>A MessageComparison object containing both versions, or a 404 if no messages are found.</returns>
        [HttpGet("compare")]
        public async Task<ActionResult<MessageComparison>> GetMessageComparison()
        {
            var comparison = await _trumpMessageService.GetMessageComparisonAsync();

            if (comparison == null)
            {
                return NotFound("No messages found with at least 1 word.");
            }

            return Ok(comparison);
        }
    }
}
