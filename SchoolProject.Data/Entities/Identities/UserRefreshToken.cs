﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProject.Data.Entities.Identities
{
    public class UserRefreshToken
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }

        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? JwtId { get; set; }

        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty(nameof(User.UserRefreshTokens))]
        public virtual User? User { get; set; }
    }
}
