using AspectInjector.Broker;
using System.Diagnostics;

namespace EcuCan.Services;

[Aspect(Scope.Global)]
public class LogAspect
{
    // ログファイルの保存場所（実行ファイルと同じフォルダに作成されます）
    private static readonly string LogFilePath = @"method_execution_log.csv";
    // ファイル書き込み時の競合を防ぐためのロックオブジェクト
    private static readonly object FileLock = new object();

    [Advice(Kind.Around, Targets = Target.Method)]
    public object HandleMethod(
        [Argument(Source.Name)] string name,           // メソッド名
        [Argument(Source.Arguments)] object[] args,    // 引数リスト
        [Argument(Source.Target)] Func<object[], object> method // 元のメソッド
    )
    {
        // 1. 計測開始
        var sw = Stopwatch.StartNew();
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string resultStatus = "Success";

        try
        {
            // 2. 元のメソッドを実行
            var result = method(args);
            return result;
        }
        catch (Exception)
        {
            // 例外が発生した場合はステータスをErrorに変更
            resultStatus = "Error";
            throw; // 例外はそのまま上位へ投げる
        }
        finally
        {
            // 3. 計測終了とログ出力（成功・失敗に関わらず必ず実行）
            sw.Stop();
            long durationMs = sw.ElapsedMilliseconds;

            WriteLogToCsv(name, timestamp, durationMs, resultStatus);
        }
    }

    private void WriteLogToCsv(string methodName, string timestamp, long duration, string status)
    {
        
        try
        {
            // CSVフォーマット: メソッド名, 日時, 実行時間(ms), 結果
            string logLine = $"{methodName},{timestamp},{duration},{status}";

            lock (FileLock)
            {
                // ファイルが存在しない場合はヘッダーを作成
                if (!File.Exists(LogFilePath))
                {
                    File.WriteAllText(LogFilePath, "MethodName,UtcTimestamp,Duration(ms),Result" + Environment.NewLine);
                }

                // ログを追記
                File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            // ログ出力自体の失敗はアプリを停止させないようコンソールに出すだけにする
            Console.WriteLine($"[LogAspect Error] Failed to write log: {ex.Message}");
        }
    }
}

// 2. トリガーとなる属性クラス
[Injection(typeof(LogAspect))]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
public class LogAttribute : Attribute
{
}