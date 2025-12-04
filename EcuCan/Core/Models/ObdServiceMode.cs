namespace EcuCan.Core.Models;

/// <summary>
/// OBD-II Service ID (SID) / Mode
/// </summary>
public enum ObdServiceMode : byte
{
    /// <summary>
    /// 現在のデータを要求 (Show Current Data)
    /// 最も一般的。回転数、速度、水温などのライブデータ取得に使用。
    /// </summary>
    CurrentData = 0x01,

    /// <summary>
    /// フリーズフレームデータを要求 (Show Freeze Frame Data)
    /// 故障発生時の瞬間データを取得。
    /// </summary>
    FreezeFrame = 0x02,

    /// <summary>
    /// 故障コードを表示 (Show Stored DTCs)
    /// </summary>
    ShowDtc = 0x03,

    /// <summary>
    /// 故障コードと保存値を消去 (Clear DTCs and Stored Values)
    /// ※チェックランプを消すコマンド。送信には注意が必要。
    /// </summary>
    ClearDtc = 0x04,

    /// <summary>
    /// O2センサーのテスト結果 (Test Results, Oxygen Sensor Monitoring)
    /// 古い規格。CAN以前の車両向け。
    /// </summary>
    TestResultO2 = 0x05,

    /// <summary>
    /// その他のシステムテスト結果 (Test Results, Other Component/System Monitoring)
    /// CAN対応車両でのモニタリング結果。
    /// </summary>
    TestResultOther = 0x06,

    /// <summary>
    /// 保留中の故障コードを表示 (Show Pending DTCs)
    /// 確定していない一時的なエラーコード。
    /// </summary>
    ShowPendingDtc = 0x07,

    /// <summary>
    /// 車両情報の要求 (Request Vehicle Information)
    /// VIN(車台番号)やキャリブレーションIDの取得。
    /// </summary>
    VehicleInfo = 0x09,

    /// <summary>
    /// 永続的な故障コードを表示 (Permanent DTCs)
    /// クリアしても消えない深刻なエラー。
    /// </summary>
    PermanentDtc = 0x0A
}