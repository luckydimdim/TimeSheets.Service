namespace Cmas.Services.TimeSheets.Dtos.Responses
{
    public class AttachmentResponse
    {
        /// <summary>
        /// Attachment name
        /// </summary>
        public string Name;

        /// <summary>
        /// Attachment MIME type
        /// </summary>
        public string Content_type;

        /// <summary>
        /// Real attachment size in bytes
        /// </summary>
        public int Length;
    }
}
