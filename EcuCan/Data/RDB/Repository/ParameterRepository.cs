using EcuCan.Core.Models;
using EcuCan.Data.RDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcuCan.Data.RDB.Repository;

public class ParameterRepository
{
    private readonly EcuDbContext _context;

    public ParameterRepository(EcuDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// DBを初期化（作成）します
    /// </summary>
    public async Task EnsureCreatedAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// 全パラメータを取得し、ドメインモデルに変換して返します
    /// </summary>
    public async Task<List<CanParameter>> GetAllAsync()
    {
        var entities = await _context.Parameters.AsNoTracking().ToListAsync();
        
        return entities.Select(e => new CanParameter(
            e.Id, e.Name, e.ServiceId, e.ParameterId, e.BytesReturned, e.Formula, e.Unit, e.Description
        )).ToList();
    }

    /// <summary>
    /// 指定されたVINに紐づく全パラメータを取得します。
    /// 該当するパラメータがない場合は空のリストを返します（またはデフォルト定義にフォールバック可能）
    /// </summary>
    public async Task<List<CanParameter>> GetVinParametersAsync(string vin)
    {
        // 1. まず指定されたVINで完全一致検索
        var entities = await _context.Parameters
            .AsNoTracking()
            .Where(p => p.Vid == vin) // ここを Vin から Vid に修正
            .ToListAsync();

        // 2. もし1件も見つからず、かつ指定VINが "DEFAULT_VIN" でない場合、
        // デフォルト設定 ("DEFAULT_VIN") を取得して返す（フォールバック）
        if (!entities.Any() && vin != "DEFAULT_VIN")
        {
            entities = await _context.Parameters
                .AsNoTracking()
                .Where(p => p.Vid == "DEFAULT_VIN") // ここも Vin から Vid に修正
                .ToListAsync();
        }

        // 3. ドメインモデルに変換して返却
        return entities.Select(e => new CanParameter(
            e.Id, 
            e.Name, 
            e.ServiceId, 
            e.ParameterId, 
            e.BytesReturned, 
            e.Formula, 
            e.Unit, 
            e.Description
        )).ToList();
    }

    /// <summary>
    /// パラメータを保存または更新します (Upsert)
    /// </summary>
    public async Task SaveAsync(CanParameter param,string vid)
    {
        var existing = await _context.Parameters.FindAsync(param.Id);

        if (existing == null)
        {
            // 新規作成
            var entity = new CanParameterEntity
            {
                Id = param.Id,
                Name = param.Name,
                Vid = vid,
                ServiceId = param.ServiceId,
                ParameterId = param.ParameterId,
                BytesReturned = param.BytesReturned,
                Formula = param.Formula,
                Unit = param.Unit,
                Description = param.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Parameters.Add(entity);
        }
        else
        {
            // 更新
            existing.Name = param.Name;
            existing.BytesReturned = param.BytesReturned;
            existing.Formula = param.Formula;
            existing.Unit = param.Unit;
            existing.Description = param.Description;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// IDで検索
    /// </summary>
    public async Task<CanParameter?> FindByIdAsync(string id)
    {
        var e = await _context.Parameters.FindAsync(id);
        if (e == null) return null;

        return new CanParameter(
            e.Id, e.Name, e.ServiceId, e.ParameterId, e.BytesReturned, e.Formula, e.Unit, e.Description
        );
    }
}