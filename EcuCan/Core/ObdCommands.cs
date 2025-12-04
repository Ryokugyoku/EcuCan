using EcuCan.Core.Models;

namespace EcuCan.Core;

/// <summary>
/// アプリケーション層 (ISO 15031-5 / SAE J1979): OBD-II サービスを定義する
/// 基本フォーマット 長さ、SID、PID、
/// </summary>
public class ObdCommands
{
    // OBD2リクエストID (標準的な 0x7DF)
    public const uint RequestId = 0x7DF;
    // OBD2レスポンスID (標準的な 0x7E8 など)
    // ※実際は 0x7E8 ～ 0x7EF の範囲で返ってくることが多い
    public const uint ResponseIdStart = 0x7E8;
    public const uint ResponseIdEnd = 0x7EF;
    
    /// <summary>
    /// サポートIDを返すためのHEXコマンド
    /// </summary>
    public static readonly CanFrame SupportedPids_01_20 = new CanFrame(
        RequestId, 
        new byte[] { 0x02, 0x01, 0x00, 0x55, 0x55, 0x55, 0x55, 0x55 }, // 0x55はパディング(埋め)
        DateTime.Now
    );
    
    /// <summary>
    /// 車台番号(VIN)をリクエストするためのHEXコマンド (Mode 09, PID 02)
    /// Data: [Length=02, Mode=09, PID=02, Padding...]
    /// </summary>
    public static readonly CanFrame RequestVin = new CanFrame(
        RequestId,
        new byte[] { 0x02, 0x09, 0x02, 0x55, 0x55, 0x55, 0x55, 0x55 }, 
        DateTime.Now
    );
}