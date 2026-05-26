using System;

namespace MySubs;

public class SubscriptionItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public double Price { get; set; }
    public string PayMethod { get; set; } = "";
    public string Currency { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CancelRequestDate { get; set; }
    public string Description { get; set; } = "";
    public bool AutoRenew { get; set; }
    
    public bool IsInTrial => TrialEndDate.HasValue && TrialEndDate.Value > DateTime.Now;
    public bool IsPendingCancel => CancelRequestDate.HasValue && (DateTime.Now - CancelRequestDate.Value).TotalMinutes < 30;
    
    public string PayMethodName => PayMethod switch
    {
        "card" => "💳",
        "crypto" => "₿",
        "foreign_card" => "🌍",
        _ => "💰"
    };
    
    public override string ToString()
    {
        string status = IsPendingCancel ? "⏳ Ожидает отмены" : 
                       !IsActive ? "❌ Отменена" :
                       IsInTrial ? "🎁 Пробный период" :
                       $"до {EndDate:dd.MM.yyyy}";
        
        string priceText = IsInTrial ? "0" : $"{Price} {Currency}";
        return $"{PayMethodName} {Title} — {priceText} [{status}]";
    }
}