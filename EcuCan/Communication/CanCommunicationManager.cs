using EcuCan.Communication.Detection;
using EcuCan.Communication.Drivers;
using EcuCan.Communication.Interfaces;

namespace EcuCan.Communication;

public class CanCommunicationManager
{
    private IHardwareConnection? _currentConnection;

    /// <summary>
    /// 自動接続ロジック
    /// </summary>
    public async Task<bool> AutoConnectAsync(CancellationToken ct = default)
    {
        // 1. 候補ポートを取得
        var candidates = PortScanner.GetCandidatePorts();

        foreach (var port in candidates)
        {
            if (ct.IsCancellationRequested) return false;

            // 2. 各ポートに対して、本当に目的のデバイスかチェック
            bool isTargetDevice = await PortScanner.CheckDeviceCapabilityAsync(port);

            if (isTargetDevice)
            {
                // 3. 接続確立
                _currentConnection = new SerialCanDriver(); // 実装クラス
                bool success = await _currentConnection.ConnectAsync(port, ct);
                if (success)
                {
                    Console.WriteLine($"Device found and connected on {port}");
                    return true;
                }
            }
        }

        Console.WriteLine("No compatible ECU interface found.");
        return false;
    }
}