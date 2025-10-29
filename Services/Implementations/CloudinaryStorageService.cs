using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DTOs.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Services.Interfaces;

namespace Services.Implementations
{
    public class CloudinaryStorageService : IImageStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryOptions _options;

        public CloudinaryStorageService(IOptions<CloudinaryOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.CloudName) || string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.ApiSecret))
            {
                throw new InvalidOperationException("Cloudinary is not configured.");
            }
            var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadAsync(IFormFile file, string? fileName = null, CancellationToken cancellationToken = default)
        {
            await using var stream = file.OpenReadStream();
            var publicId = string.IsNullOrWhiteSpace(fileName) ? Guid.NewGuid().ToString("N") : fileName;
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = _options.FolderPrefix.Trim('/'),
                PublicId = publicId,
                UseFilename = false,
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            if (uploadResult.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            {
                return uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty;
            }
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error?.Message}");
        }
    }
}


