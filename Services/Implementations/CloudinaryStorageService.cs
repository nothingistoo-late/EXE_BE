using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DTOs.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Services.Implementations
{
    public class CloudinaryStorageService : IImageStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryOptions _options;
        private readonly ILogger<CloudinaryStorageService> _logger;

        public CloudinaryStorageService(IOptions<CloudinaryOptions> options, ILogger<CloudinaryStorageService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            if (string.IsNullOrWhiteSpace(_options.CloudName) || string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.ApiSecret))
            {
                throw new InvalidOperationException("Cloudinary is not configured.");
            }
            var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadAsync(IFormFile file, string? fileName = null, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting image upload: {FileName}, Size: {Size} bytes", file.FileName, file.Length);

            try
            {
                byte[] optimizedImageBytes;
                var fileSize = file.Length;
                var shouldOptimize = fileSize > 50000; // Only optimize if file > 50KB
                
                if (shouldOptimize)
                {
                    var optimizationStartTime = DateTime.UtcNow;
                    
                    // Read file stream once and process
                    await using var originalStream = file.OpenReadStream();
                    using var image = await Image.LoadAsync(originalStream, cancellationToken);
                    
                    var originalWidth = image.Width;
                    var originalHeight = image.Height;
                    
                    // Resize if image is too large (max 800x800 for avatars)
                    var maxDimension = 800;
                    if (image.Width > maxDimension || image.Height > maxDimension)
                    {
                        var ratio = Math.Min((double)maxDimension / image.Width, (double)maxDimension / image.Height);
                        var newWidth = (int)(image.Width * ratio);
                        var newHeight = (int)(image.Height * ratio);
                        
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new SixLabors.ImageSharp.Size(newWidth, newHeight),
                            Mode = ResizeMode.Max
                        }));
                        
                        _logger.LogInformation("Image resized from {OriginalWidth}x{OriginalHeight} to {NewWidth}x{NewHeight}", 
                            originalWidth, originalHeight, newWidth, newHeight);
                    }

                    // Save optimized image directly to memory stream as JPEG with quality 85
                    using var memoryStream = new MemoryStream();
                    await image.SaveAsJpegAsync(memoryStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                    {
                        Quality = 85
                    }, cancellationToken);
                    
                    optimizedImageBytes = memoryStream.ToArray();
                    
                    var optimizationTime = (DateTime.UtcNow - optimizationStartTime).TotalMilliseconds;
                    _logger.LogInformation("Image optimized in {OptimizationMs}ms: Original size: {OriginalSize} bytes, Optimized size: {OptimizedSize} bytes", 
                        optimizationTime, fileSize, optimizedImageBytes.Length);
                }
                else
                {
                    // For small files, read directly without processing
                    await using var stream = file.OpenReadStream();
                    using var memoryStream = new MemoryStream((int)fileSize + 1024); // Pre-allocate capacity
                    await stream.CopyToAsync(memoryStream, cancellationToken);
                    optimizedImageBytes = memoryStream.ToArray();
                }

                // Upload optimized image to Cloudinary
                var uploadStartTime = DateTime.UtcNow;
                var publicId = string.IsNullOrWhiteSpace(fileName) ? Guid.NewGuid().ToString("N") : fileName;
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, new MemoryStream(optimizedImageBytes)),
                    Folder = _options.FolderPrefix.Trim('/'),
                    PublicId = publicId,
                    UseFilename = false,
                    Overwrite = true,
                    // Additional optimization settings
                    Transformation = new Transformation()
                        .Quality("auto") // Auto quality optimization
                        .FetchFormat("auto") // Auto format (WebP if supported)
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
                
                var uploadTime = (DateTime.UtcNow - uploadStartTime).TotalMilliseconds;
                var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                if (uploadResult.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
                {
                    var url = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty;
                    _logger.LogInformation("Image uploaded successfully - Total: {TotalMs}ms (Upload: {UploadMs}ms), Size: {Size} bytes, URL: {Url}", 
                        totalTime, uploadTime, optimizedImageBytes.Length, url);
                    return url;
                }
                
                _logger.LogError("Cloudinary upload failed after {ElapsedMs}ms: {Error}", totalTime, uploadResult.Error?.Message);
                throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error?.Message}");
            }
            catch (OperationCanceledException)
            {
                var elapsedTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("Image upload cancelled after {ElapsedMs}ms: {FileName}", elapsedTime, file.FileName);
                throw;
            }
            catch (Exception ex)
            {
                var elapsedTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogError(ex, "Error uploading image after {ElapsedMs}ms: {FileName}", elapsedTime, file.FileName);
                throw;
            }
        }
    }
}


