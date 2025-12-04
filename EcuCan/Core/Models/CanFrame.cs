namespace EcuCan.Core.Models;

public record CanFrame(
    uint Id,            // CAN ID
    byte[] Data,        // データ本体 (最大8byte)
    DateTime Timestamp  // 受信時刻
);