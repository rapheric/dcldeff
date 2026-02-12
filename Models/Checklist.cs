using System.ComponentModel.DataAnnotations;

namespace NCBA.DCL.Models;

public class Checklist
{
    // Draft support
    public string? DraftDataJson { get; set; }
    public bool IsDraft { get; set; } = false;
    public DateTime? DraftExpiresAt { get; set; }
    public DateTime? DraftLastSaved { get; set; }

    public Guid Id { get; set; }

    [Required]
    public string DclNo { get; set; } = string.Empty;

    // Customer details
    public Guid? CustomerId { get; set; }
    public User? Customer { get; set; }

    [Required]
    public string CustomerNumber { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    // Loan info
    public string? LoanType { get; set; }

    // Assignments
    public Guid? AssignedToRMId { get; set; }
    public User? AssignedToRM { get; set; }

    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public Guid? AssignedToCoCheckerId { get; set; }
    public User? AssignedToCoChecker { get; set; }

    // Main Status
    public ChecklistStatus Status { get; set; } = ChecklistStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Track who last updated the checklist
    public Guid? LastUpdatedBy { get; set; }

    // Navigation properties
    public string? IbpsNo { get; set; }
    public string? Remarks { get; set; }
    public string? GeneralComment { get; set; }
    public string? CheckerComment { get; set; }
    public string? FinalComment { get; set; }
    public bool SubmittedToCoChecker { get; set; } = false;

    // Navigation properties
    public ICollection<DocumentCategory> Documents { get; set; } = new List<DocumentCategory>();
    public ICollection<ChecklistLog> Logs { get; set; } = new List<ChecklistLog>();
    public ICollection<SupportingDoc> SupportingDocs { get; set; } = new List<SupportingDoc>();
}

public class SupportingDoc
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? FileType { get; set; }

    public Guid? UploadedById { get; set; }
    public User? UploadedBy { get; set; }
    public string? UploadedByRole { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Guid ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;
}

public class ChecklistLog
{
    public Guid Id { get; set; }

    public string? Message { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Guid ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;
}

public enum ChecklistStatus
{
    CoCreatorReview,
    RMReview,
    CoCheckerReview,
    Approved,
    Rejected,
    Active,
    Completed,
    Pending
}
