using System.IO.Ports;
using EcuCan.Communication.Interfaces;
using EcuCan.Core.Models;

namespace EcuCan.Communication.Drivers;

internal class SerialCanDriver : IHardwareConnection
{
    private SerialPort? _serialPort;

    public bool IsConnected => _serialPort is { IsOpen: true };
    
    public event Action<CanFrame>? FrameReceived;
    
    public Task<bool> ConnectAsync(string portName, CancellationToken ct = default)
    {
        if (IsConnected) Disconnect();

        try
        {
            _serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;
            _serialPort.DataReceived += OnDataReceived;
            _serialPort.Open();

            // ▼ 追加: CANアダプタの初期化シーケンス (SLCANプロトコルの一般的例)
            // デバイスによってはコマンドが異なる場合があります
            
            // 1. まずクローズコマンドを送って設定モードにする
            _serialPort.Write("C\r"); 
            Thread.Sleep(100); 

            // 2. 通信速度を設定 (S5 = 500kbps, S6 = 1Mbps など)
            // 一般的な乗用車は500kbpsが多いです
            _serialPort.Write("S5\r"); 
            Thread.Sleep(100);

            // 3. CANバスをオープン (データが流れ始めます)
            _serialPort.Write("O\r");
            Thread.Sleep(100);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to {portName}: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public void Disconnect()
    {
        if (_serialPort != null)
        {
            // ▼ 追加: イベント解除（メモリリーク防止）
            _serialPort.DataReceived -= OnDataReceived;

            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
            _serialPort.Dispose();
            _serialPort = null;
        }
    }

    /// <summary>
    /// データ受信時
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort == null || !_serialPort.IsOpen) return;

        try
        {
            // 1. 生データを読み込む
            // 注意: 多くのCAN-USB変換機はテキストベース(SLCAN)かバイナリ形式でデータを送ってきます。
            // ここでは「改行区切りのテキスト」で送られてくると仮定した簡易実装です。
            string rawData = _serialPort.ReadLine(); 

            // 2. パース処理（本来は専用のParserクラスに委譲すべき）
            // 例: "123,AA,BB" みたいなCSV形式など、デバイス仕様に依存します
            var frame = ParseCanFrame(rawData); 
            
            // 3. 上位層に通知
            if (frame != null)
            {
                FrameReceived?.Invoke(frame);
            }
        }
        catch (Exception)
        {
            // 読み込みエラーやタイムアウトのハンドリング
        }
    }
    
    public void Dispose()
    {
        Disconnect();
    }
    
    // ▼ 追加: 送信処理
    public Task SendFrameAsync(CanFrame frame)
    {
        if (!IsConnected) return Task.CompletedTask;

        // CanFrame -> SLCAN文字列変換
        // 例: ID=123, Data=AA,BB -> "t1232AABB\r"
        
        string idHex = frame.Id.ToString("X3"); // 123
        string length = frame.Data.Length.ToString("X1"); // 2
        string dataHex = BitConverter.ToString(frame.Data).Replace("-", ""); // AABB
        
        string slcanString = $"t{idHex}{length}{dataHex}\r";

        _serialPort?.Write(slcanString);
        return Task.CompletedTask;
    }

    /// <summary>
    /// SLCANプロトコル
    /// </summary>
    /// <param name="rawLine"></param>
    /// <returns></returns>
    private CanFrame? ParseCanFrame(string rawLine)
    {
        // 1. 空文字やノイズの除去
        if (string.IsNullOrWhiteSpace(rawLine)) return null;
        var line = rawLine.Trim(); // 前後の空白や改行コードを削除

        // 2. フォーマットチェック
        // 't' で始まるものは標準フレーム(11bit ID)
        // 'T' で始まるものは拡張フレーム(29bit ID) ですが、今回は標準のみ実装します
        if (line.Length < 5 || line[0] != 't') 
        {
            // 対応していない形式、またはゴミデータ
            return null;
        }

        try
        {
            // 例: "t1232AABB"
                
            // 3. CAN ID の取得 (1文字目から3文字分) -> "123"
            string idHex = line.Substring(1, 3);
            uint id = Convert.ToUInt32(idHex, 16); // 16進数文字列を数値に変換

            // 4. データ長 (DLC) の取得 (4文字目から1文字分) -> "2"
            // SLCANではここが0-8の数字になります
            int length = int.Parse(line.Substring(4, 1));
                
            // 文字列の長さが足りているかチェック（データ部が存在するか）
            if (line.Length < 5 + (length * 2)) return null;

            // 5. データバイトのパース
            byte[] data = new byte[length];
            for (int i = 0; i < length; i++)
            {
                // データ開始位置(5) + (何バイト目か * 2文字) から2文字切り出す
                // "AA" や "BB" を取得
                string byteHex = line.Substring(5 + (i * 2), 2);
                data[i] = Convert.ToByte(byteHex, 16);
            }

            // 6. オブジェクト生成して返す
            return new CanFrame(id, data, DateTime.Now);
        }
        catch (Exception)
        {
            // パース中にエラー（16進数でない文字が含まれていた等）が発生した場合は無視
            return null;
        }
    }
}