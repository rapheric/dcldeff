using Microsoft.AspNetCore.Http;
using System;
using System.Text.Json.Serialization;

namespace NCBA.DCL.DTOs
{
    public class FileUploadDto
    {
        public required IFormFile File { get; set; }
        public string? ChecklistId { get; set; }
        public string? DocumentId { get; set; }
        public string? DocumentName { get; set; }
        public string? Category { get; set; }
    }
}
