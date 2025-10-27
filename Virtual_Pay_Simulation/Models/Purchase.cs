using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Virtual_Pay_Simulation.Models
{
    public class Purchase
    {
        [Key]
        public int PurchaseId { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser ApplicationUser { get; set; }

        [Required]
        public decimal PaidAmount { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; }

        // Bu alanda, satın alınan tüm seansların ID'lerini virgülle ayrılmış bir string olarak saklayacağız.
        [Required]
        public string SessionIds { get; set; }
    }
}
