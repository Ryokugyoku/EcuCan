using System.Collections.Concurrent;
using EcuCan.Core.Models;

namespace EcuCan.Core;

/// <summary>
/// 検出されたPIDを管理するレジストリ
/// </summary>
public class DetectedPidRegistry
{
    // スレッドセーフな辞書 (Key: "SID-PID" 例: "01-0C")
    private readonly ConcurrentDictionary<string, CanParameter> _registry = new();

    /// <summary>
    /// PIDを登録または更新します
    /// </summary>
    /// <param name="sid">Service ID</param>
    /// <param name="pid">Parameter ID</param>
    /// <param name="dataLength">データ長</param>
    /// <param name="vid">VIN</param>
    public void Register(byte sid, byte pid, int dataLength,string vid)
    {
        string key = GenerateKey(sid, pid);

        // 既に登録済みなら何もしない（重複登録防止）
        if (_registry.ContainsKey(key)) return;

        // 新規発見！まずは「Unknown」として登録
        var param = new CanParameter(
            Id: key,
            Name: $"Unknown PID {pid:X2} (SID:{sid:X2})", // とりあえず仮名
            Vin:vid,
            ServiceId: sid,
            ParameterId: pid,
            BytesReturned: dataLength,
            Formula: "A",          // 仮の計算式 (生データAを表示)
            Unit: "",              // 単位なし
            Description: "Auto-detected via Brute-force Scan"
        );

        _registry.TryAdd(key, param);
    }

    /// <summary>
    /// 外部ファイル等から読み込んだ定義情報で、既存のUnknownパラメータを上書き更新する
    /// </summary>
    public void UpdateDefinition(CanParameter definedParam)
    {
        string key = GenerateKey(definedParam.ServiceId, definedParam.ParameterId);
        
        // キーが存在すれば上書き、なければ新規追加
        _registry.AddOrUpdate(key, definedParam, (k, oldVal) => definedParam);
    }

    /// <summary>
    /// 現在登録されている全パラメータを取得
    /// </summary>
    public IEnumerable<CanParameter> GetAll() => _registry.Values;

    /// <summary>
    /// 特定のSID/PID定義を取得
    /// </summary>
    public CanParameter? Find(byte sid, byte pid)
    {
        string key = GenerateKey(sid, pid);
        _registry.TryGetValue(key, out var param);
        return param;
    }

    private string GenerateKey(byte sid, byte pid) => $"{sid:X2}-{pid:X2}";
}