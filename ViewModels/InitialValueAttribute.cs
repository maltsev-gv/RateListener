#nullable enable
using System;

namespace RateListener.ViewModels;

/// <summary>
/// Присваивает начальное значение свойству класса - наследника ObservableObject
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
// ReSharper disable once ClassNeverInstantiated.Global
public class InitialValueAttribute(object? value) : Attribute
{
    public object? InitialValue { get; } = value;
}
