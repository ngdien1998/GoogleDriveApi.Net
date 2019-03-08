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
    public static class GoogleDriveFilesRepositoryService
    {
        private static readonly string[] scopes = { DriveService.Scope.DriveFile };
        private static readonly string applicationName = "Drive API API Version 3";

        /// <summary>
        /// 
        /// </summary>
        private static DriveService GetGoogleDriveService()
        {
            UserCredential credential;

            using (FileStream stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
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
        /// 
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<GoogleDriveFileEntity> GetGoogleDriveFiles()
        {
            // Define parameters of request
            FilesResource.ListRequest listRequest = GetGoogleDriveService().Files.List();
            listRequest.Fields = "nextPageToken, files(id, name, size, version, createdTime)";

            // List files
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;
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
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="filePath"></param>
        public static void UploadFile(string filePath)
        {
            var fileMetaData = new Google.Apis.Drive.v3.Data.File
            {
                Name = Path.GetFileName(filePath),
                MimeType = MimeTypes.GetMimeType(filePath)
            };

            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                FilesResource.CreateMediaUpload request = GetGoogleDriveService().Files.Create(fileMetaData, stream, fileMetaData.MimeType);
                request.Fields = "id";
                request.Upload();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public static async Task<string> DownloadFile(string fileId)
        {
            FilesResource.GetRequest request = GetGoogleDriveService().Files.Get(fileId);

            string fileName = request.Execute().Name;
            string filePath = Path.Combine(Path.GetTempPath(), fileName);

            MemoryStream stream = new MemoryStream();

            request.MediaDownloader.ProgressChanged += progress =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                        break;
                    case DownloadStatus.Completed:
                        SaveFile(stream, filePath);
                        break;
                    case DownloadStatus.Failed:
                        break;
                }
            };

            await request.DownloadAsync(stream);
            return filePath;
        }

        public static void DeleteFile(string fileId)
        {
            GetGoogleDriveService().Files.Delete(fileId).Execute();
        }

        private static void SaveFile(MemoryStream stream, string filePath)
        {
            using (FileStream fstream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                stream.WriteTo(fstream);
            };
        }
    }
}