using Microsoft.Extensions.DependencyInjection;
using EcuCan.Services;
using EcuCan.Data.RDB;
using EcuCan.Data.RDB.Repository;
using EcuCan.Communication;
using EcuCan.Communication.Interfaces;
using EcuCan.Communication.Drivers;
using EcuCan.Core;

namespace EcuCan.Extensions;

/// <summary>
/// Provides extension methods for configuring the EcuCan library in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// EcuCanライブラリのサービスをDIコンテナに登録します
    /// </summary>
    public static IServiceCollection AddEcuCanLibrary(this IServiceCollection services)
    {
        // 1. DBコンテキストの登録
        services.AddDbContext<EcuDbContext>();

        // 2. リポジトリの登録
        services.AddScoped<ParameterRepository>();

        // 3. コア機能の登録
        services.AddSingleton<DetectedPidRegistry>();
        
        // 4. 通信層の登録
        // ※Manager経由でConnectionを取得する構造の場合、SingletonでManagerを登録
        services.AddSingleton<CanCommunicationManager>();
        
        // IHardwareConnection は Manager から取得するのか、直接注入するのか設計次第ですが、
        // 簡易的に SerialCanDriver を直接登録する場合:
        services.AddTransient<IHardwareConnection, SerialCanDriver>();

        // 5. 上位サービスの登録
        services.AddTransient<EcuDataService>();

        return services;
    }
}