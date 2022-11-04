using System.ComponentModel.DataAnnotations;
using Devon4Net.Application.WebAPI.Implementation.Domain.Entities;
using Devon4Net.Application.WebAPI.Implementation;
namespace Devon4Net.Application.WebAPI.Implementation.Business.SessionManagement.Dto
{
    /// <summary>
    /// Session definition
    /// </summary>
    public class SessionDto
    {
        /// <summary>
        /// the Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// the Expiry Date
        /// </summary>
        [Required]
        public DateTime ExpiresAt  { get; set; }

    }
}


    