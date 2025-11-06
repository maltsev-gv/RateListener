using System;

namespace RateListener.Models;

public class SettingsInfo
{
    public Guid Id { get; set; }
    public string BankProviderName { get; set; }
    public string SearchFromCurr { get; set; }
    public string SearchToCurr { get; set; }
    public string FromFee { get; set; }
    public string ToFee { get; set; }
    public bool IsToFeeIncluded { get; set; }
    public bool IsFromFeeIncluded { get; set; }
    public bool IsImprovementAlert { get; set; }
    public bool IsDepreciationAlert { get; set; }
    public bool IsAlertWhenMoreChecked { get; set; }
    public string AlertWhenMore { get; set; }
    public bool IsAlertWhenLessChecked { get; set; }
    public string AlertWhenLess { get; set; }
    public double LastEffectiveRate { get; set; }
    public string SellingAmount { get; set; }
    public string BuyingAmount { get; set; }
}