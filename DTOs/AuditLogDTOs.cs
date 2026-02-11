using System;
using System.Text.Json.Serialization;

namespace NCBA.DCL.DTOs
{
    public class AuditLogCreateDto
    {
        public string Action { get; set; } = string.Empty;
        public string? Resource { get; set; }
        public string? ResourceId { get; set; }
        public string? Status { get; set; }
        public string? Details { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid? PerformedById { get; set; }
        public Guid? TargetUserId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}