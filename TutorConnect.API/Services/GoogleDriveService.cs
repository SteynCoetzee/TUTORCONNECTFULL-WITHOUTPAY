using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System.Text.Json;

namespace TutorConnect.API.Services
{
    public class GoogleDriveService
    {
        private readonly DriveService _driveService;
        private readonly string _folderId;

        public GoogleDriveService(IConfiguration config)
        {
            var credentialsPath = config["GoogleDrive:CredentialsPath"]!;
            _folderId = config["GoogleDrive:FolderId"]!;

            var json = File.ReadAllText(credentialsPath);
            var doc = JsonDocument.Parse(json).RootElement;
            var clientEmail = doc.GetProperty("client_email").GetString()!;
            var privateKey  = doc.GetProperty("private_key").GetString()!;

            var credential = new ServiceAccountCredential(
                new ServiceAccountCredential.Initializer(clientEmail)
                {
                    Scopes = new[] { DriveService.ScopeConstants.Drive }
                }.FromPrivateKey(privateKey)
            );

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "TutorConnect"
            });
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string mimeType)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                Parents = new[] { _folderId }
            };

            var request = _driveService.Files.Create(fileMetadata, fileStream, mimeType);
            request.Fields = "id";

            var progress = await request.UploadAsync();

            if (progress.Status != Google.Apis.Upload.UploadStatus.Completed)
                throw new Exception($"Google Drive upload failed: {progress.Exception?.Message ?? "Unknown error"}");

            if (request.ResponseBody == null)
                throw new Exception("Google Drive upload completed but returned no file ID.");

            var fileId = request.ResponseBody.Id;

            var permission = new Google.Apis.Drive.v3.Data.Permission
            {
                Type = "anyone",
                Role = "reader"
            };
            await _driveService.Permissions.Create(permission, fileId).ExecuteAsync();

            return $"https://drive.google.com/uc?export=view&id={fileId}";
        }
    }
}
