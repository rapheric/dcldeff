using System.Text.Json.Serialization;

namespace NCBA.DCL.DTOs
{
    public class RegisterAdminDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginAdminDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class CreateUserDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
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
