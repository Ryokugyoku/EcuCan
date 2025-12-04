using EcuCan.Core.Models;

namespace EcuCan.Communication.Interfaces;

public interface IHardwareConnection : IDisposable
{
    bool IsConnected { get; }
    Task<bool> ConnectAsync(string portName, CancellationToken ct = default);
    void Disconnect();
    
    /// <summary>
    /// データ受信時の
    /// </summary>
    event Action<CanFrame>? FrameReceived;
    
    /// <summary>
    /// データを送信するための定義
    /// </summary>
    /// <param name="frame"></param>
    /// <returns></returns>
    Task SendFrameAsync(CanFrame frame);
}