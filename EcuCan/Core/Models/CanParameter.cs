using System;

namespace EcuCan.Core.Models;

/// <summary>
/// CANで取得できるパラメータの定義情報
/// </summary>
public record CanParameter(
    string Id,           // ユニークID (例: "01-0C" または "EngineSpeed")
    string Name,         // 表示名 (例: "エンジン回転数", "Unknown PID 0C")
    string Vin,
    byte ServiceId,      // SID (Service ID / Mode)
    byte ParameterId,    // PID (Parameter ID)
    int BytesReturned,   // 想定されるデータバイト数 (例: 2バイト)
    string Formula,      // 計算式文字列 (例: "(A*256+B)/4")
    string Unit,         // 単位 (例: "rpm")
    string Description = "" // 説明 (任意)
)
{
    /// <summary>
    /// 生データ(byte配列)を計算式に基づいて物理値(double)に変換します
    /// </summary>
    public double Calculate(byte[]? data)
    {
        // データが空の場合は0を返す
        if (data == null || data.Length == 0) return 0.0;

        // 本来はここで NCalc などの数式エンジンを使って Formula 文字列を評価すべきですが、
        // 今回は標準的な計算パターンを簡易的にハードコーディングで分岐させます。

        double a = data.Length > 0 ? data[0] : 0;
        double b = data.Length > 1 ? data[1] : 0;
        // double c = data.Length > 2 ? data[2] : 0;
        // double d = data.Length > 3 ? data[3] : 0;

        // 簡易実装: Formula文字列に含まれるパターンで分岐
        // ※実運用ではここをちゃんとしたパーサーに置き換えてください

        // 例: 回転数 "(A*256+B)/4"
        if (Formula.Contains("/4") && BytesReturned == 2)
        {
            return (a * 256 + b) / 4.0;
        }
        // 例: 水温 "A-40"
        else if (Formula.Contains("-40"))
        {
            return a - 40.0;
        }
        // 例: 割合 "A/2.55" (0-100%)
        else if (Formula.Contains("/2.55"))
        {
            return a / 2.55;
        }
        // デフォルト: 1バイト目をそのまま返す
        else
        {
            return a;
        }
    }
}