using Repositories.Models;
using Repositories.CRUD.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Analytics;

public interface IDailyBalanceDBService
{
	Task<DailyEnergyBalance> CreateAsync(DailyEnergyBalance balance, CancellationToken cancellationToken = default);
	Task<DailyEnergyBalance?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
	Task<IEnumerable<DailyEnergyBalance>> GetAllAsync(CancellationToken cancellationToken = default);
	Task<DailyEnergyBalance> UpdateAsync(DailyEnergyBalance balance, CancellationToken cancellationToken = default);
	Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class DailyBalanceDBService : IDailyBalanceDBService
{
	private readonly IDailyEnergyBalanceRepository _repository;

	public DailyBalanceDBService(IDailyEnergyBalanceRepository repository)
	{
		_repository = repository;
	}

	public async Task<DailyEnergyBalance> CreateAsync(DailyEnergyBalance balance, CancellationToken cancellationToken = default)
	{
		return await _repository.AddAsync(balance, cancellationToken);
	}

	public async Task<DailyEnergyBalance?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
	{
		return await _repository.GetByIdAsync(id, cancellationToken);
	}

	public async Task<IEnumerable<DailyEnergyBalance>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		return await _repository.GetAllAsync(cancellationToken);
	}

	public async Task<DailyEnergyBalance> UpdateAsync(DailyEnergyBalance balance, CancellationToken cancellationToken = default)
	{
		return await _repository.UpdateAsync(balance, cancellationToken);
	}

	public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
	{
		return await _repository.DeleteAsync(id, cancellationToken);
	}
}
