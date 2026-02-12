using System.Text.Json.Serialization;

namespace NCBA.DCL.DTOs
{
    public class RegisterAdminDto
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class LoginAdminDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class CreateUserDto
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Role { get; set; }
    }

    public class TransferRoleDto
    {
        public string NewRole { get; set; } = string.Empty;
    }

    public class ReassignTasksDto
    {
        public string ToUserId { get; set; } = string.Empty;
    }

    public class IdResponseDto
    {
        [JsonPropertyName("id")]
        public Guid? Id { get; set; }

        [JsonPropertyName("_id")]
        public Guid? _id { get; set; }

        // Helper property
        public Guid? DocumentId => Id ?? _id;
    }
}
