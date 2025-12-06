using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    // 1. This is the NEW class to define a single contact.
    public class ContactPerson
    {
        [Required]
        [Display(Name = "Contact Name")]
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress]
        [RegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = null!;

        [Required]
        [Phone]
        [RegularExpression(@"^(\+63|0)9\d{9}$", ErrorMessage = "Invalid Philippine phone number. (e.g., 09xxxxxxxxx or +639xxxxxxxxx)")]
        public string Phone { get; set; } =null!;
    }
}

