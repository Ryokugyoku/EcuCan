using EcuCan.Communication.Interfaces;
using EcuCan.Core;
using EcuCan.Core.Models;
using EcuCan.Data.RDB.Repository;

namespace EcuCan.Services;

/// <summary>
/// CANバスを介してECU (Engine Control Unit) と通信する機能を提供します。
/// このサービスは、ECUとの接続の初期化、車両識別番号 (VIN) の取得、
/// およびサポートされているPID (Parameter Identifier) のスキャンを担当します。
/// </summary>
public class EcuDataService
{
    private readonly IHardwareConnection _connection;
    private readonly ParameterRepository _repository; // 追加: DB操作用

    private List<CanParameter> _pidsList;
    string VehicleIdentificationNumber { get; set; } = "UNKNOWN";

    // コンストラクタ注入で Repository を受け取る
    public EcuDataService(IHardwareConnection connection, ParameterRepository repository)
    {
        _connection = connection;
        _repository = repository;
        _pidsList = _repository.GetAllAsync().Result;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (!_connection.IsConnected) throw new InvalidOperationException("Hardware connection is not established.");

        try
        {
            // ① VIN取得
            var vinResponse = await RequestAndWaitAsync(ObdCommands.RequestVin, 1000, ct);
            if (vinResponse != null)
            {
                // 簡易解析 (本来はISO-TP)
                // とりあえず応答があればVINとして扱う
                VehicleIdentificationNumber = BitConverter.ToString(vinResponse.Data);
                _pidsList = await _repository.GetVinParametersAsync(VehicleIdentificationNumber);

                if (_pidsList.Count > 0)
                {
                    return;
                }
            }
            else
            {
                // タイムアウト時はデフォルト値
                VehicleIdentificationNumber = "DEFAULT_VIN";
            }

            // ② PID一覧取得
            var pidResponse = await RequestAndWaitAsync(ObdCommands.SupportedPids_01_20, 1000, ct);
            if (pidResponse != null)
            {
                var pids = ParseSupportedPids(pidResponse.Data);
                
                // ★ここでDBに登録する
                await RegisterPidsToDatabaseAsync(VehicleIdentificationNumber, pids);
                _pidsList = await _repository.GetVinParametersAsync(VehicleIdentificationNumber);
            }

            Console.WriteLine("EcuDataService Initialized.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Initialization failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 検出したPIDリストをDBに「未定義(Unknown)」として登録する
    /// </summary>
    private async Task RegisterPidsToDatabaseAsync(string vin, List<byte> pids)
    {
        // DB初期化確認
        await _repository.EnsureCreatedAsync();

        foreach (var pid in pids)
        {
            string key = $"01-{pid:X2}"; // ID生成ルール (SID-PID)

            // 既に存在するかチェック (FindByIdAsync に VIN を渡すオーバーロードが必要)
            // ここではシンプルに「新規作成」用オブジェクトを作ってSaveを呼ぶ
            // Repository側で「あれば更新、なければ作成」してくれる実装になっている前提
            
            var param = new CanParameter(
                Id: key,
                Name: $"Unknown PID {pid:X2}", // 初期名
                Vin:vin,
                ServiceId: 0x01,
                ParameterId: pid,
                BytesReturned: 1, // 仮 (実際はPIDごとに異なる)
                Formula: "A",     // 仮
                Unit: "",
                Description: "Auto-detected"
            );

            // VIN付きで保存
            await _repository.SaveAsync( param,vin);
        }
        
        Console.WriteLine($"Registered {pids.Count} PIDs for VIN: {vin}");
    }
    /// <summary>
    /// リクエストを送信し、対応するレスポンスを待ちます (汎用メソッド)
    /// </summary>
    private async Task<CanFrame?> RequestAndWaitAsync(CanFrame request, int timeoutMs, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<CanFrame>();

        // 応答IDの範囲を計算 (例: 7DF -> 7E8..7EF)
        // リクエストデータから期待する SID と PID を抽出
        byte reqSid = request.Data[1]; // 例: 01
        byte reqPid = request.Data[2]; // 例: 00

        void Handler(CanFrame res)
        {
            // チェック条件:
            // 1. IDが ECU応答範囲 (0x7E8 ～ 0x7EF)
            // 2. SID が (RequestSID + 0x40) (例: 01 -> 41)
            // 3. PID が一致
            if (res.Id >= 0x7E8 && res.Id <= 0x7EF &&
                res.Data.Length >= 3 &&
                res.Data[1] == (reqSid + 0x40) &&
                res.Data[2] == reqPid)
            {
                tcs.TrySetResult(res);
            }
        }

        try
        {
            _connection.FrameReceived += Handler;
            await _connection.SendFrameAsync(request);

            // タイムアウト設定
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            
            using (cts.Token.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }
        catch (TaskCanceledException)
        {
            return null; // タイムアウト
        }
        finally
        {
            _connection.FrameReceived -= Handler;
        }
    }

    /// <summary>
    /// PID 00 (Supported PIDs) の応答データ(4バイト)を解析して、有効なPIDリストを返します
    /// </summary>
    private List<byte> ParseSupportedPids(byte[] data)
    {
        var list = new List<byte>();
        
        // データフォーマット: [Len, 41, 00, A, B, C, D, ...]
        if (data.Length < 7) return list;

        // A, B, C, D の各バイトをチェック
        // A: PID 01-08
        // B: PID 09-10
        // C: PID 11-18
        // D: PID 19-20
        
        byte[] masks = { data[3], data[4], data[5], data[6] };
        
        for (int byteIndex = 0; byteIndex < 4; byteIndex++)
        {
            byte mask = masks[byteIndex];
            for (int bitIndex = 0; bitIndex < 8; bitIndex++)
            {
                // 最上位ビット(MSB)が PID x1, 最下位ビット(LSB)が PID x8
                bool isSupported = (mask & (1 << (7 - bitIndex))) != 0;
                
                if (isSupported)
                {
                    // PID番号の計算: (バイト位置 * 8) + ビット位置 + 1
                    int pidNum = (byteIndex * 8) + bitIndex + 1;
                    list.Add((byte)pidNum);
                }
            }
        }

        return list;
    }
}