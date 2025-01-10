namespace videomasking.Services.Contracts
{
    public interface IVideoProcessingService
    {
        Task StreamProcessedVideo(HttpResponse response);

    }
}
