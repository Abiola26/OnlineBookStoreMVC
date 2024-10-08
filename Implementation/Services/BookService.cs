﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OnlineBookStoreMVC.DTOs;
using OnlineBookStoreMVC.Entities;
using OnlineBookStoreMVC.Implementation.Interface;
using OnlineBookStoreMVC.Models;
using OnlineBookStoreMVC.Models.RequestModels;

namespace OnlineBookStoreMVC.Implementation.Services
{
    //[Authorize(Roles = "SuperAdmin")]

    public class BookService : IBookService
    {
        private readonly ApplicationDbContext _context;

        public BookService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BookDto>> GetAllBooksAsync()
        {
            var books = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .ToListAsync();

            return books.Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description,
                ISBN = b.ISBN,
                Publisher = b.Publisher,
                PublicationDate = b.PublicationDate,
                Price = b.Price,
                AuthorId = b.AuthorId,
                AuthorName = b.Author.Name,
                CategoryId = b.CategoryId,
                CategoryName = b.Category.Name,
                Reviews = b.Reviews.Select(r => new ReviewDto
                {
                    Id = r.Id,
                    Rating = r.Rating,
                    Comment = r.Comment
                }).ToList(),
                CoverImageUrl = b.CoverImageUrl,
                Pages = b.Pages,
                Language = b.Language,
                TotalQuantity = b.TotalQuantity, // Assuming you have a Quantity property in your Book entity
                RemainingQuantity = b.RemainingQuantity - _context.OrderItems
                                                     .Where(oi => oi.BookId == b.Id)
                                                     .Sum(oi => oi.Quantity)
            });
        }

        public async Task<BookDto> GetBookByIdAsync(Guid id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null) return null;

            return new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Description = book.Description,
                ISBN = book.ISBN,
                Publisher = book.Publisher,
                PublicationDate = book.PublicationDate,
                Price = book.Price,
                AuthorName = book.Author.Name,
                CategoryName = book.Category.Name,
                Reviews = book.Reviews.Select(r => new ReviewDto
                {
                    Id = r.Id,
                    Rating = r.Rating,
                    Comment = r.Comment
                }).ToList(),
                CoverImageUrl = book.CoverImageUrl,
                Pages = book.Pages,
                Language = book.Language,
                TotalQuantity = book.TotalQuantity,
                RemainingQuantity = book.RemainingQuantity - _context.OrderItems
                                                     .Where(oi => oi.BookId == book.Id)
                                                     .Sum(oi => oi.Quantity)
            };
        }

        public async Task<BookDto> CreateBookAsync(BookRequestModel bookRequest)
        {
            try
            {
                string coverImageUrl = null;
                if (bookRequest.CoverImageFile != null && bookRequest.CoverImageFile.Length > 0)
                {
                    coverImageUrl = await SaveFileAsync(bookRequest.CoverImageFile);
                }

                var book = new Book
                {
                    Title = bookRequest.Title,
                    Description = bookRequest.Description,
                    ISBN = bookRequest.ISBN,
                    Publisher = bookRequest.Publisher,
                    PublicationDate = bookRequest.PublicationDate,
                    Price = bookRequest.Price,
                    AuthorId = bookRequest.AuthorId,
                    CategoryId = bookRequest.CategoryId,
                    CoverImageUrl = coverImageUrl,
                    Pages = bookRequest.Pages,
                    Language = bookRequest.Language,
                    TotalQuantity = bookRequest.TotalQuantity
                };

                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                return new BookDto
                {
                    Id = book.Id,
                    Title = book.Title,
                    Description = book.Description,
                    ISBN = book.ISBN,
                    Publisher = book.Publisher,
                    PublicationDate = book.PublicationDate,
                    Price = book.Price,
                    CoverImageUrl = book.CoverImageUrl,
                    Pages = book.Pages,
                    Language = book.Language,
                    TotalQuantity = book.TotalQuantity,
                    RemainingQuantity = book.RemainingQuantity
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<BookDto> UpdateBookAsync(Guid id, BookRequestModel bookRequest)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return null;

            book.Title = bookRequest.Title;
            book.Description = bookRequest.Description;
            book.ISBN = bookRequest.ISBN;
            book.Publisher = bookRequest.Publisher;
            book.PublicationDate = bookRequest.PublicationDate;
            book.Price = bookRequest.Price;
            book.AuthorId = bookRequest.AuthorId;
            book.CategoryId = bookRequest.CategoryId;
            book.Pages = bookRequest.Pages;
            book.Language = bookRequest.Language;

            if (bookRequest.CoverImageFile != null && bookRequest.CoverImageFile.Length > 0)
            {
                book.CoverImageUrl = await SaveFileAsync(bookRequest.CoverImageFile);
            }

            // If the quantity is updated, ensure the remaining quantity is adjusted accordingly
            var previousQuantity = book.TotalQuantity;
            book.TotalQuantity = bookRequest.TotalQuantity;

            _context.Books.Update(book);
            await _context.SaveChangesAsync();

            var totalOrdersForBook = _context.OrderItems
                                             .Where(oi => oi.BookId == id)
                                             .Sum(oi => oi.Quantity);

            return new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Description = book.Description,
                ISBN = book.ISBN,
                Publisher = book.Publisher,
                PublicationDate = book.PublicationDate,
                Price = book.Price,
                CoverImageUrl = book.CoverImageUrl,
                Pages = book.Pages,
                Language = book.Language,
                TotalQuantity = book.TotalQuantity,
                RemainingQuantity = book.RemainingQuantity - totalOrdersForBook
            };
        }
        public async Task DeleteBookAsync(Guid id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            var uploads = Path.Combine("wwwroot", "images");
            if (!Directory.Exists(uploads))
            {
                Directory.CreateDirectory(uploads);
            }
            var filePath = Path.Combine(uploads, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/images/{file.FileName}";
        }
    }
}


//public class BookService : IBookService
//{
//    private readonly ApplicationDbContext _context;

//    public BookService(ApplicationDbContext context)
//    {
//        _context = context;
//    }

//    public async Task<IEnumerable<BookDto>> GetAllBooksAsync()
//    {
//        var books = await _context.Books
//            .Include(b => b.Author)
//            .Include(b => b.Category)
//            .Include(b => b.Reviews)
//            .ToListAsync();

//        return books.Select(b => new BookDto
//        {
//            Id = b.Id,
//            Title = b.Title,
//            Description = b.Description,
//            ISBN = b.ISBN,
//            Publisher = b.Publisher,
//            PublicationDate = b.PublicationDate,
//            Price = b.Price,
//            AuthorId = b.AuthorId,
//            AuthorName = b.Author.Name,
//            CategoryId = b.CategoryId,
//            CategoryName = b.Category.Name,
//            Reviews = b.Reviews.Select(r => new ReviewDto
//            {
//                Id = r.Id,
//                Rating = r.Rating,
//                Comment = r.Comment
//            }).ToList(),
//            CoverImageUrl = b.CoverImageUrl,
//            Pages = b.Pages,
//            Language = b.Language
//        });
//    }

//    public async Task<BookDto> GetBookByIdAsync(Guid id)
//    {
//        var book = await _context.Books
//            .Include(b => b.Author)
//            .Include(b => b.Category)
//            .Include(b => b.Reviews)
//            .FirstOrDefaultAsync(b => b.Id == id);

//        if (book == null) return null;

//        return new BookDto
//        {
//            Id = book.Id,
//            Title = book.Title,
//            Description = book.Description,
//            ISBN = book.ISBN,
//            Publisher = book.Publisher,
//            PublicationDate = book.PublicationDate,
//            Price = book.Price,
//            AuthorName = book.Author.Name,
//            CategoryName = book.Category.Name,
//            Reviews = book.Reviews.Select(r => new ReviewDto
//            {
//                Id = r.Id,
//                Rating = r.Rating,
//                Comment = r.Comment
//            }).ToList(),
//            CoverImageUrl = book.CoverImageUrl,
//            Pages = book.Pages,
//            Language = book.Language
//        };
//    }

//    public async Task<BookDto> CreateBookAsync(BookRequestModel bookRequest)
//    {
//        try
//        {
//            string coverImageUrl = null;
//            if (bookRequest.CoverImageFile != null && bookRequest.CoverImageFile.Length > 0)
//            {
//                coverImageUrl = await SaveFileAsync(bookRequest.CoverImageFile);
//            }

//            var book = new Book
//            {
//                Title = bookRequest.Title,
//                Description = bookRequest.Description,
//                ISBN = bookRequest.ISBN,
//                Publisher = bookRequest.Publisher,
//                PublicationDate = bookRequest.PublicationDate,
//                Price = bookRequest.Price,
//                AuthorId = bookRequest.AuthorId,
//                CategoryId = bookRequest.CategoryId,
//                CoverImageUrl = coverImageUrl,
//                Pages = bookRequest.Pages,
//                Language = bookRequest.Language
//            };

//            _context.Books.Add(book);
//            await _context.SaveChangesAsync();

//            return new BookDto
//            {
//                Id = book.Id,
//                Title = book.Title,
//                Description = book.Description,
//                ISBN = book.ISBN,
//                Publisher = book.Publisher,
//                PublicationDate = book.PublicationDate,
//                Price = book.Price,
//                CoverImageUrl = book.CoverImageUrl,
//                Pages = book.Pages,
//                Language = book.Language
//            };
//        }
//        catch (Exception ex)
//        {
//           throw;
//        }

//    }

//    public async Task<string> SaveFileAsync(IFormFile file)
//    {
//        var uploads = Path.Combine("wwwroot", "images");
//        if (!Directory.Exists(uploads))
//        {
//            Directory.CreateDirectory(uploads);
//        }
//        var filePath = Path.Combine(uploads, file.FileName);
//        using (var stream = new FileStream(filePath, FileMode.Create))
//        {
//            await file.CopyToAsync(stream);
//        }
//        return $"/images/{file.FileName}";
//    }


//    public async Task<BookDto> UpdateBookAsync(Guid id, BookRequestModel bookRequest)
//    {
//        var book = await _context.Books.FindAsync(id);
//        if (book == null) return null;

//        book.Title = bookRequest.Title;
//        book.Description = bookRequest.Description;
//        book.ISBN = bookRequest.ISBN;
//        book.Publisher = bookRequest.Publisher;
//        book.PublicationDate = bookRequest.PublicationDate;
//        book.Price = bookRequest.Price;
//        book.AuthorId = bookRequest.AuthorId;
//        book.CategoryId = bookRequest.CategoryId;
//        book.Pages = bookRequest.Pages;
//        book.Language = bookRequest.Language;

//        if (bookRequest.CoverImageFile != null && bookRequest.CoverImageFile.Length > 0)
//        {
//            book.CoverImageUrl = await SaveFileAsync(bookRequest.CoverImageFile);
//        }

//        _context.Books.Update(book);
//        await _context.SaveChangesAsync();

//        return new BookDto
//        {
//            Id = book.Id,
//            Title = book.Title,
//            Description = book.Description,
//            ISBN = book.ISBN,
//            Publisher = book.Publisher,
//            PublicationDate = book.PublicationDate,
//            Price = book.Price,
//            CoverImageUrl = book.CoverImageUrl,
//            Pages = book.Pages,
//            Language = book.Language
//        };
//    }

//    public async Task<bool> DeleteBookAsync(Guid id)
//    {
//        var book = await _context.Books.FindAsync(id);
//        if (book == null) return false;

//        _context.Books.Remove(book);
//        await _context.SaveChangesAsync();

//        return true;
//    }

//}