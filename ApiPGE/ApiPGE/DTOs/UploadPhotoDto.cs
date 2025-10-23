namespace Api.Dtos
{
    public class UploadPhotoDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Base64Content { get; set; } = string.Empty;
    }

    public class UploadPhotoResponse
    {
        public string PhotoUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}