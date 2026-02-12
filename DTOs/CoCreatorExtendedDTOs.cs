using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using NCBA.DCL.Models;

namespace NCBA.DCL.DTOs;

public class UpdateChecklistWithDocsRequest
{
    public ChecklistStatus? Status { get; set; }
    public string? GeneralComment { get; set; }
    public List<DocumentCategoryDto>? Documents { get; set; }
}

public class DocumentCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public List<DocumentDto> DocList { get; set; } = new();
}

public class DocumentDto
{
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    [JsonPropertyName("_id")]
    public Guid? _id { get; set; }

    public string? FileUrl { get; set; }
    public string? Comment { get; set; }
    public DocumentStatus? Status { get; set; }
    public CreatorStatus? CreatorStatus { get; set; }
    public string? DeferralReason { get; set; }
    public string? DeferralNumber { get; set; }

    // Helper property to resolve either id or _id
    public Guid? DocumentId => Id ?? _id;
}


