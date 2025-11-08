using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Service.Interface;

namespace Service.Servicefolder
{
    public class FileUploadService : IFileUploadService
    {
        private readonly Cloudinary _cloudinary;

        public FileUploadService(IConfiguration config)
        {
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription($"{DateTime.UtcNow:yyyyMMddHHmmss}_{file.FileName}", stream),
                Folder = "challenges",
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true,
                Type = "upload",
                AccessMode = "public" // ✅ thêm dòng này
            };
            // DÙNG RawUploadAsync chứ KHÔNG dùng UploadAsync

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, "auto");


            return uploadResult?.SecureUrl?.ToString() ?? string.Empty;
        }
    }
}
