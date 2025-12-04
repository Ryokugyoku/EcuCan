using System.IO.Ports;

namespace EcuCan.Communication.Detection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

internal static class PortScanner
{
        /// <summary>
    /// 現在の環境で使用可能なCANアダプタ候補のポートリストを返します
    /// </summary>
    public static IEnumerable<string> GetCandidatePorts()
    {
        var allPorts = SerialPort.GetPortNames();
        var platform = GetCurrentPlatform();

        // OSごとにフィルタリング（無関係なポートへのアクセスを減らすため）
        return platform switch
        {
            OsPlatformType.Windows => allPorts.Where(p => p.StartsWith("COM")),
            // macOSは /dev/tty.usbserial-XXXX や /dev/cu.usbmodem-XXXX など
            OsPlatformType.MacoS => allPorts.Where(p => Regex.IsMatch(p, @"/dev/tty\.(usb|cu).*")),
            // Linuxは /dev/ttyUSB0 や /dev/ttyACM0 など
            OsPlatformType.Linux => allPorts.Where(p => Regex.IsMatch(p, @"/dev/tty(USB|ACM).*")),
            _ => allPorts
        };
    }

    /// <summary>
    /// 指定したポートに目的のデバイス（ECU通信機）がいるかチェックします
    /// </summary>
    public static async Task<bool> CheckDeviceCapabilityAsync(string portName)
    {
        try
        {
            using var port = new SerialPort(portName, 115200); // 機器に合わせたBaudRate
            port.ReadTimeout = 500;
            port.WriteTimeout = 500;
            port.Open();

            // 【重要】ハンドシェイク処理
            // 単にポートが開けただけでは不十分なため、
            // 特定のコマンド（例: "PING"）を送り、期待する応答（例: "PONG"）が来るか確認します。
            
            // port.WriteLine("PING");
            // await Task.Delay(100);
            // string response = port.ReadExisting();
            // return response.Contains("ACK_CAN_ADAPTER");
            
            // ※現時点ではポートオープン成功をもって「候補」とみなす仮実装
            return true; 
        }
        catch
        {
            // アクセス拒否やタイムアウト時は対象外とみなす
            return false;
        }
    }

    private enum OsPlatformType { Windows, MacoS, Linux, Unknown }

    private static OsPlatformType GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return OsPlatformType.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return OsPlatformType.MacoS;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return OsPlatformType.Linux;
        return OsPlatformType.Unknown;
    }
}