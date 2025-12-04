using EcuCan.Communication.Interfaces;
using EcuCan.Core;
using EcuCan.Core.Models;

namespace EcuCan.Services;

/// <summary>
/// ECUに対して総当たり（ブルートフォース）でリクエストを送り、
/// 応答可能なSID/PIDを探索するサービス
/// </summary>
internal class EcuBruteForceScannerService
{
    private readonly IHardwareConnection _connection;
    private readonly DetectedPidRegistry _registry;
    
    // 存在するすべてのセンサー機器のデータを抽出
    private readonly byte[] _targetServiceIds;

    /// <summary>
    /// ECUに対して総当たり（ブルートフォース）でリクエストを送り、
    /// 応答可能なSID/PIDを探索するサービス
    /// </summary>
    public EcuBruteForceScannerService(IHardwareConnection connection, DetectedPidRegistry registry,
        ObdServiceMode targetMode)
    {
        _connection = connection;
        _registry = registry;
        _targetServiceIds = new[] { (byte)targetMode };
    }

    /// <summary>
    /// ECUに対して総当たり（ブルートフォース）でリクエストを送り、
    /// 応答可能なSID/PIDを探索するサービス
    /// </summary>
    /// <remarks>
    /// 指定されたECUに対し、複数のSIDを用いてリクエストを送信し、
    /// 応答を解析して利用可能なPIDを特定するためのサービス。
    /// </remarks>
    public EcuBruteForceScannerService(IHardwareConnection connection, DetectedPidRegistry registry,
        ObdServiceMode[] targetModes)
    {
        _connection = connection;
        _registry = registry;
        // Enum配列を byte配列に変換して保持
        _targetServiceIds = targetModes.Select(m => (byte)m).ToArray();
    }

    /// <summary>
    /// 指定されたSID(デフォルトは主要SID)に対して、PID 00～FFを総当たりでスキャンします。
    /// 応答があったものは DetectedPidRegistry に登録されます。
    /// </summary>
    public async Task RunBruteForceScanAsync(CancellationToken ct = default)
    {
        foreach (byte sid in _targetServiceIds) 
        {
            if (ct.IsCancellationRequested) break;

            Console.WriteLine($"--- Brute-force Scanning SID: {sid:X2} ---");

            // PID のループ (00 ～ FF)
            for (int pid = 0; pid <= 0xFF; pid++)
            {
                if (ct.IsCancellationRequested) break;
                
                byte targetPid = (byte)pid;

                // 1. リクエスト送信 & 応答確認
                var result = await TryRequestAsync(sid, targetPid, ct);

                // 2. 応答があれば登録
                if (result != null)
                {
                    // ヘッダ(3byte)を除いた有効データ長
                    int dataLen = Math.Max(0, result.RawData.Length - 3);
                    
                    // "Unknown" としてレジストリに登録
                    _registry.Register(sid, targetPid, dataLen);
                    
                    Console.WriteLine($"[FOUND] SID:{sid:X2} PID:{targetPid:X2} DataLen:{dataLen}");
                }

                // バス負荷を考慮して少し待機 (15ms)
                await Task.Delay(15, ct);
            }
        }
    }

    /// <summary>
    /// 単発のリクエストを送り、応答を待ちます
    /// </summary>
    private async Task<ScanResult?> TryRequestAsync(byte sid, byte pid, CancellationToken ct)
    {
        // リクエスト: [02, SID, PID, 00...] (OBD標準フォーマット)
        var frame = new CanFrame(0x7DF, new byte[] { 0x02, sid, pid, 0x00, 0x00, 0x00, 0x00, 0x00 }, DateTime.Now);
        var tcs = new TaskCompletionSource<byte[]>();

        // 応答ハンドラ
        void Handler(CanFrame res)
        {
            // 応答チェック: 
            // ID: 7E8-7EF (ECU応答範囲)
            // SID: リクエストSID + 0x40 (例: 01 -> 41)
            // PID: リクエストPIDと一致
            if (res.Id >= 0x7E8 && res.Id <= 0x7EF &&
                res.Data.Length >= 3 &&
                res.Data[1] == (sid + 0x40) &&
                res.Data[2] == pid)
            {
                tcs.TrySetResult(res.Data);
            }
        }

        try
        {
            _connection.FrameReceived += Handler;
            await _connection.SendFrameAsync(frame);

            // タイムアウト: 100ms (総当たりなので高速化のため短めに設定)
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(100);
            
            using (cts.Token.Register(() => tcs.TrySetCanceled()))
            {
                byte[] data = await tcs.Task;
                return new ScanResult(sid, pid, data, true);
            }
        }
        catch (TaskCanceledException)
        {
            return null; // 応答なし
        }
        finally
        {
            _connection.FrameReceived -= Handler;
        }
    }
}