using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using GoogleDriveApi.Models.DataModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleDriveApi.Models.BusinessModels
{
    /// <summary>
    /// Manages file in google drive
    /// </summary>
    public static class GoogleDriveFilesRepositoryService
    {
        private static readonly string[] scopes = { DriveService.Scope.DriveFile };
        private static readonly string applicationName = "Nhom 2";

        /// <summary>
        /// Get instance of Google.Apis.Drive.v3.DriveService after configuring user authentication
        /// </summary>
        private static DriveService GetGoogleDriveService()
        {
            UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "SavedToken";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // Create Drive API service.
            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });
        }

        /// <summary>
        /// Get google drive files
        /// </summary>
        /// <returns>List of google drive files</returns>
        public static IEnumerable<GoogleDriveFileEntity> GetGoogleDriveFiles()
        {
            IList<Google.Apis.Drive.v3.Data.File> files = null;

            // Define parameters of request
            FilesResource.ListRequest listRequest = GetGoogleDriveService().Files.List();
            listRequest.Fields = "nextPageToken, files(id, name, size, version, createdTime)";

            // List files
            files = listRequest.Execute().Files;

            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    yield return new GoogleDriveFileEntity
                    {
                        Id = file.Id,
                        Name = file.Name,
                        Size = file.Size,
                        Version = file.Version,
                        CreatedTime = file.CreatedTime
                    };
                }
            }
        }

        /// <summary>
        /// Upload file to google drive
        /// </summary>
        /// <param name="filePath">Upload file path in server</param>
        public static void UploadFile(string filePath)
        {
            var fileMetaData = new Google.Apis.Drive.v3.Data.File
            {
                Name = Path.GetFileName(filePath),
                MimeType = MimeTypes.GetMimeType(filePath)
            };

            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                var request = GetGoogleDriveService().Files.Create(fileMetaData, stream, fileMetaData.MimeType);
                request.Fields = "id";
                request.Upload();
            }
            File.Delete(filePath);
        }

        /// <summary>
        /// Download file from google drive
        /// </summary>
        /// <param name="fileId">Downloaded file ID</param>
        /// <returns>Server file path in server</returns>
        public static async Task<string> DownloadFileAsync(string fileId)
        {
            FilesResource.GetRequest request = GetGoogleDriveService().Files.Get(fileId);

            string fileName = request.Execute().Name;
            string filePath = Path.Combine(Path.GetTempPath(), fileName);

            MemoryStream stream = new MemoryStream();

            request.MediaDownloader.ProgressChanged += progress =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Completed:
                        SaveFile(stream, filePath);
                        break;
                    case DownloadStatus.Downloading:
                    case DownloadStatus.Failed:
                        break;
                }
            };

            await request.DownloadAsync(stream);
            return filePath;
        }

        /// <summary>
        /// Delete file from google drive
        /// </summary>
        /// <param name="fileId">Google drive file ID</param>
        public static void DeleteFile(string fileId)
        {
            GetGoogleDriveService().Files.Delete(fileId).Execute();
        }

        /// <summary>
        /// Save file to server
        /// </summary>
        /// <param name="stream">Memory stream containing file</param>
        /// <param name="filePath">File path in server</param>
        private static void SaveFile(MemoryStream stream, string filePath)
        {
            using (FileStream fstream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                stream.WriteTo(fstream);
            };
        }
    }
}