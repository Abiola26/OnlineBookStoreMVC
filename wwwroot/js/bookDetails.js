<script>
    document.addEventListener('DOMContentLoaded', function () {
            // Initialize Delete Modal
            const deleteModal = document.getElementById('deleteModal');
    deleteModal.addEventListener('show.bs.modal', function (event) {
                const button = event.relatedTarget;
    const bookId = button.getAttribute('data-book-id');
    const bookIdInput = document.getElementById('bookIdToDelete');
    bookIdInput.value = bookId;
            });

    // Initialize Details Modal
    const detailsModal = document.getElementById('detailsModal');
    detailsModal.addEventListener('show.bs.modal', async function (event) {
                const button = event.relatedTarget;
    const bookId = button.getAttribute('data-book-id');

    // Fetch book details from the server
    const response = await fetch(`/Book/Details/${bookId}`);
    const book = await response.json();

    // Check if the book exists
    if (!book) {
                    return;
                }

    // Populate the modal with book details
    const detailsDiv = document.getElementById('bookDetails');
    detailsDiv.innerHTML = `
    <img src="${book.coverImageUrl}" alt="${book.title}" class="img-fluid mb-3">
        <h4>${book.title}</h4>
        <p><strong>Author:</strong> ${book.author}</p>
        <p><strong>Category:</strong> ${book.categoryName}</p>
        <p><strong>ISBN:</strong> ${book.isbn}</p>
        <p><strong>Publisher:</strong> ${book.publisher}</p>
        <p><strong>Price:</strong> ₦${book.price.toFixed(2)}</p>
        <p><strong>Language:</strong> ${book.language}</p>
        <p><strong>Pages:</strong> ${book.pages}</p>
        <p><strong>Status:</strong> ${book.totalQuantity <= 0 ? "Out of Stock" : "In Stock"}</p>
        <p><strong>Remaining Quantity:</strong> ${book.totalQuantity}</p>
        <h5>Reviews:</h5>
        <ul>
            ${book.reviews && book.reviews.length > 0
                ? book.reviews.map(r => `<li>${r.rating} stars - ${r.comment}</li>`).join('')
                : '<li>No reviews yet</li>'}
        </ul>
        `;
            });
        });
</script>