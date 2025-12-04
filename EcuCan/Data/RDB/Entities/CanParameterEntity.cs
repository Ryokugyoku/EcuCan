namespace EcuCan.Data.RDB.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
[Table("Parameters")]
public class CanParameterEntity
{
    /// <summary>
    /// プライマリキー (例: "01-0C")
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;
    [Key]
    public string Vid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    
    public byte ServiceId { get; set; }
    
    public byte ParameterId { get; set; }
    
    public int BytesReturned { get; set; }
    
    public string Formula { get; set; } = string.Empty;
    
    public string Unit { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 作成日時
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新日時
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}