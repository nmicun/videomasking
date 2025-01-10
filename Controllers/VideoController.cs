using Microsoft.AspNetCore.Mvc;
using System.IO;
using videomasking.Services.Contracts;

namespace videomasking.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VideoController : ControllerBase
    {
        private readonly IVideoProcessingService _videoService;

        public VideoController(IVideoProcessingService videoService)
        {
            _videoService = videoService;
        }

        [HttpGet("stream")]
        public async Task GetProcessedStream()
        {
            // Postavljamo response na nivo HTTP-a za streaming
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");
            Response.Headers.Add("Content-Type", "multipart/x-mixed-replace; boundary=frame");

            // Streamovanje obrađenog videa
            await _videoService.StreamProcessedVideo(Response);
        }

    }
}
