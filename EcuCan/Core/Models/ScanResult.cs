namespace EcuCan.Core.Models;

/// <summary>
/// PIDスキャンの結果を保持するクラス
/// </summary>
public record ScanResult(
    byte ServiceId,      // リクエストしたSID (例: 0x01)
    byte ParameterId,    // リクエストしたPID (例: 0x0C)
    byte[] RawData,      // ECUから返ってきた生データ (例: 04 41 0C 1A F8 ...)
    bool IsSupported     // 応答があったかどうか
);