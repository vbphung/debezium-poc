using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace debezium_poc
{
    [Table("book_loans", Schema = "business")]
    public class BookLoan
    {
        [Column("loan_id")]
        public Guid LoanId { get; set; }

        [Column("book_id")]
        public Guid BookId { get; set; }

        [Column("mid")]
        public DateTime Mid { get; set; }

        [Column("is_valid")]
        public bool IsValid { get; set; }
    }

    class BookService(
        BusinessDbContext sourceDb,
        ILogger<BookService> logger) : IHostedService
    {
        private BusinessDbContext SourceDb => sourceDb;
        private DbSet<BookLoan> Loans => SourceDb.Set<BookLoan>();
        private ILogger<BookService> Logger => logger;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var l1 = new BookLoan
            {
                LoanId = Guid.NewGuid(),
                BookId = Guid.NewGuid(),
                Mid = DateTime.UtcNow,
                IsValid = true
            };
            await InsertAsync(l1);

            var l2 = new BookLoan
            {
                LoanId = Guid.NewGuid(),
                BookId = l1.BookId,
                Mid = l1.Mid.AddDays(10),
                IsValid = true
            };
            await InsertAsync(l2);

            await UpdateMidAsync(l2.LoanId, l1.Mid.AddDays(12));
            await UpdateBookIdAsync(l2.LoanId, Guid.NewGuid());
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task InsertAsync(BookLoan loan)
        {
            await Loans.AddAsync(loan);
            await ReconsiderAsync(loan, null);
            await SourceDb.SaveChangesAsync();

            Logger.LogInformation("Loan {Loan} was inserted",
                JsonSerializer.Serialize(loan));
        }

        private async Task UpdateBookIdAsync(Guid loanId, Guid bookId)
        {
            var loan = await Loans.FindAsync(loanId);
            if (loan == null)
            {
                return;
            }

            var oldBookId = loan.BookId;

            await MakeChangeDirectlyAsync(() =>
            {
                loan.BookId = bookId;
                return Task.CompletedTask;
            });

            Logger.LogInformation("Updated loan {LoanId}'s book to {BookId}",
                loanId, bookId);

            await MakeChangeDirectlyAsync(async () =>
            {
                await ReconsiderAsync(loan, new()
                {
                    LoanId = loan.LoanId,
                    BookId = oldBookId,
                    Mid = loan.Mid,
                    IsValid = loan.IsValid
                });
            });
        }

        private async Task UpdateMidAsync(Guid loanId, DateTime mid)
        {
            var loan = await Loans.FindAsync(loanId);
            if (loan == null)
            {
                return;
            }

            var oldMid = loan.Mid;

            await MakeChangeDirectlyAsync(() =>
            {
                loan.Mid = mid;
                return Task.CompletedTask;
            });

            Logger.LogInformation("Updated loan {LoanId}'s mid to {Mid}",
                loanId, mid);

            await MakeChangeDirectlyAsync(async () =>
            {
                await ReconsiderAsync(loan, new()
                {
                    LoanId = loan.LoanId,
                    BookId = loan.BookId,
                    Mid = oldMid,
                    IsValid = loan.IsValid
                });
            });
        }

        private async Task ReconsiderAsync(BookLoan @new, BookLoan? old)
        {
            if (old is not null)
            {
                var oldDups = await GetAllDuplicatesAsync(old);
                foreach (var loan in oldDups)
                {
                    if (!loan.IsValid)
                    {
                        await RecalculateAsync(loan);
                    }
                }
            }

            var newDups = await GetAllDuplicatesAsync(@new);
            @new.IsValid = !newDups.Any();

            foreach (var loan in newDups)
            {
                if (loan.IsValid)
                {
                    await RecalculateAsync(loan);
                }
            }
        }

        private async Task RecalculateAsync(BookLoan loan)
        {
            var dups = await GetAllDuplicatesAsync(loan);
            loan.IsValid = !dups.Any();

            Logger.LogWarning("Loan {LoanId} has been recalculated to {Loan}",
                loan.LoanId, JsonSerializer.Serialize(loan));
        }

        private async Task<IEnumerable<BookLoan>> GetAllDuplicatesAsync(BookLoan loan)
        {
            return await Loans
                .Where(l => l.LoanId != loan.LoanId)
                .Where(l => l.BookId == loan.BookId)
                .Where(l => l.Mid >= loan.Mid.AddDays(-14))
                .Where(l => l.Mid <= loan.Mid.AddDays(14))
                .ToListAsync();
        }

        private async Task MakeChangeDirectlyAsync(Func<Task> change)
        {
            await change();
            await SourceDb.SaveChangesAsync();
        }
    }
}
