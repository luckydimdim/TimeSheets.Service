using Nancy;

namespace Cmas.Services.TimeSheets.Dtos.Requests
{
    public class FileUploadRequest
    {
        public string Title { get; set; }

        public string Description { get; set; }
        
        public HttpFile File { get; set; }
    }
}
