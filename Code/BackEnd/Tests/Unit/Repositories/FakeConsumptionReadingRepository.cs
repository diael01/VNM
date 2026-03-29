using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repositories.Models;

namespace BackEnd.Tests.Unit.Repositories;

public class FakeConsumptionReadingRepository
{
    private readonly VnmDbContext _context;
    public FakeConsumptionReadingRepository(VnmDbContext context) => _context = context;
    public async Task<ConsumptionReading> AddAsync(ConsumptionReading reading)
    {
        _context.ConsumptionReadings.Add(reading);
        await _context.SaveChangesAsync();
        return reading;
    }
    public async Task<ConsumptionReading?> GetByIdAsync(int id)
        => await _context.ConsumptionReadings.FindAsync(id);
    public async Task<IEnumerable<ConsumptionReading>> GetAllAsync()
        => await _context.ConsumptionReadings.ToListAsync();
    public async Task<ConsumptionReading> UpdateAsync(ConsumptionReading reading)
    {
        _context.ConsumptionReadings.Update(reading);
        await _context.SaveChangesAsync();
        return reading;
    }
    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _context.ConsumptionReadings.FindAsync(id);
        if (entity == null) return false;
        _context.ConsumptionReadings.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<IEnumerable<ConsumptionReading>> GetLatestReadingsAsync(int count)
    {
        return await _context.ConsumptionReadings
            .AsNoTracking()
            .OrderByDescending(r => r.Timestamp)
            .Take(count)
            .ToListAsync();
    }
}

public class FakeConsumptionReadingService
{
    private readonly FakeConsumptionReadingRepository _repo;
    public FakeConsumptionReadingService(FakeConsumptionReadingRepository repo) => _repo = repo;
    public Task<ConsumptionReading> CreateAsync(ConsumptionReading r) => _repo.AddAsync(r);
    public Task<ConsumptionReading?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
    public Task<IEnumerable<ConsumptionReading>> GetAllAsync() => _repo.GetAllAsync();
    public Task<ConsumptionReading> UpdateAsync(ConsumptionReading r) => _repo.UpdateAsync(r);
    public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
}
