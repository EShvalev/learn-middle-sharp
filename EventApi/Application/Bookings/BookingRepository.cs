using EventApi.DataAсcess;
using EventApi.Models.Bookings;
using EventApi.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace EventApi.Application.Bookings
{
    public class BookingRepository : IBookingRepository
    {
        private readonly ConcurrentDictionary<Guid, Booking> _bookings = new ConcurrentDictionary<Guid, Booking>();

        private readonly AppDbContext _dbContext;

        public BookingRepository(
            AppDbContext appDbContext
            )
        {
            _dbContext = appDbContext;
        }

        public async Task CreateAsync(Booking booking, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(booking);

            if (await _dbContext.Events.FindAsync(booking.Id, ct) != null)
            {
                throw new InvalidOperationException($"Бронирование с ID {booking.Id} уже существует.");
            }

            await _dbContext.Bookings.AddAsync(booking, ct);
        }

        public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _dbContext.Bookings.FindAsync(id, ct);
        }

        public async Task UpdateAsync(Guid id, Booking booking, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(booking);

            if (await _dbContext.Bookings.FindAsync(id, ct) == null)
                throw new NotFoundException($"Бронирование {id} не найдено.");

            _dbContext.Bookings.Update(booking);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            Booking? booking = await _dbContext.Bookings.FindAsync(id, ct);
            if (booking == null)
                throw new NotFoundException($"Бронирование {id} не найдено.");

            _dbContext.Bookings.Remove(booking);
        }

        public async Task<IEnumerable<Booking>> GetAllAsync(CancellationToken ct = default) =>
            _dbContext.Bookings
                .AsNoTracking()
                .AsQueryable();
    }
}
