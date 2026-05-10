using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace TutorConnect.API.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        }

        public async Task<string> UploadImageAsync(Stream fileStream, string fileName)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = "tutorconnect-media",
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

            return result.SecureUrl.ToString();
        }

        public string GetSignedDownloadUrl(string cloudinaryUrl)
        {
            // Extract everything after /raw/upload/v{version}/
            var match = System.Text.RegularExpressions.Regex.Match(
                cloudinaryUrl, @"/raw/upload/(?:v\d+/)?(.+)$");
            if (!match.Success) return cloudinaryUrl;

            var publicIdWithExt = match.Groups[1].Value;

            // Cloudinary stores raw public_ids WITHOUT the extension —
            // the extension in the delivery URL is the format, not the ID
            var dot = publicIdWithExt.LastIndexOf('.');
            var publicId = dot > 0 ? publicIdWithExt[..dot] : publicIdWithExt;

            return _cloudinary.DownloadPrivate(publicId, resourceType: "raw");
        }

        public async Task<string> UploadRawAsync(Stream fileStream, string fileName)
        {
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = "tutorconnect-files",
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false,
                Type = "upload"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

            return result.SecureUrl.ToString();
        }

        public async Task<string> UploadVideoAsync(Stream fileStream, string fileName)
        {
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = "tutorconnect-media",
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

            return result.SecureUrl.ToString();
        }
    }
}
