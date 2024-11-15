using OnlineBookStoreMVC.Entities;
using System.ComponentModel.DataAnnotations;

namespace OnlineBookStoreMVC.Models.RequestModels
{
    public class BookRequestModel : BaseEntity
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot be longer than 1000 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "ISBN is required")]
        [StringLength(9, ErrorMessage = "ISBN must be 9 characters", MinimumLength = 9)]
        public string ISBN { get; set; }
        [Required]
        [Display(Name = "List Price")]
        [Range(1, 1000)]
        public decimal ListPrice { get; set; }

        [Required]
        [Display(Name = "Price for 1-50")]
        [Range(1, 1000)]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Price for 50+")]
        [Range(1, 1000)]
        public decimal Price50 { get; set; }

        [Required]
        [Display(Name = "Price for 100+")]
        [Range(1, 1000)]
        public decimal Price100 { get; set; }

        [Required(ErrorMessage = "Author is required")]
        [StringLength(100, ErrorMessage = "Author name cannot be longer than 100 characters")]
        public string Author { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public Guid CategoryId { get; set; }

        public IFormFile CoverImageFile { get; set; }

        [Required(ErrorMessage = "Number of pages is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Pages must be a positive number")]
        public int Pages { get; set; }

        //public IFormFile File { get; set; }

        [Required(ErrorMessage = "Language is required")]
        [StringLength(50, ErrorMessage = "Language cannot be longer than 50 characters")]
        public string Language { get; set; }

        [Required(ErrorMessage = "Total quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Total quantity must be a non-negative number")]
        public int TotalQuantity { get; set; }
    }
}
