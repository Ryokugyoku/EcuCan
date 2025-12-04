namespace EcuCan.Data.RDB;
using EcuCan.Data.RDB.Entities;
using Microsoft.EntityFrameworkCore;

public class EcuDbContext : DbContext
{
    public DbSet<CanParameterEntity> Parameters { get; set; }

    // 接続文字列やDBファイルパスの設定
    // ※実運用ではコンストラクタでOptionを受け取るパターンが一般的ですが、
    // 簡易利用のためにOnConfiguringで指定します。
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // 実行ファイルと同じ場所に ecu_master.db を作成
            optionsBuilder.UseSqlite("Data Source=ecu_master.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ユニーク制約などの追加設定があればここに記述
        base.OnModelCreating(modelBuilder);
    }
}