#nullable enable
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using RateListener.ExtensionMethods;

namespace RateListener.ViewModels;

/// <summary>
/// Реализует хранение значений свойств и быстрый доступ к ним из дочерних классов. Реализует INotifyPropertyChanged. Изменение свойства инициирует вызов PropertyChanged
/// </summary>
[Serializable]
public class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public event PropertyChangingEventHandler? PropertyChanging;

    public void VerifyPropertyName([CallerMemberName] string? propertyName = null)
    {
        var myType = GetType();
        if (!string.IsNullOrEmpty(propertyName) && myType.GetProperty(propertyName) == null)
            throw new ArgumentException("Property not found", nameof(propertyName));
    }

    protected void RaisePropertyChanging([CallerMemberName] string? propertyName = null)
    {
#if DEBUG
        VerifyPropertyName(propertyName);
#endif
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
    }

    protected void RaisePropertyChanging<T>(Expression<Func<T>> propertyExpression)
    {
        var propertyName = GetPropertyName(propertyExpression);
        RaisePropertyChanging(propertyName);
    }

    protected virtual bool IsTraceEnabled => false;

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (PropertyChanged == null)
            return;

        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        if (!string.IsNullOrEmpty(propertyName) && !propDict.ContainsKey(propertyName))
        {
            var pi = GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            propDict[propertyName] = pi != null
                ? pi.GetValue(this, null)
                : throw new ArgumentException("Property not found", nameof(propertyName));
        }
        if (IsTraceEnabled)
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                var propVal = propDict[propertyName];
                var strVal = propVal == null
                    ? "null"
                    : $"{propVal}, hash = {propVal.GetHashCode()}";
                Console.WriteLine($"RaisePropertyChanged: {propertyName} is now \"{strVal}\"");
            }
            else
                Console.WriteLine($"RaisePropertyChanged: All properties updated");
        }
    }

    protected void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
    {
        var handler = PropertyChanged;
        if (handler != null)
        {
            var propertyName = GetPropertyName(propertyExpression);
            RaisePropertyChanged(propertyName);
        }
    }

    protected static string GetPropertyName<T>(Expression<Func<T>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);

        if (propertyExpression.Body is not MemberExpression body)
            throw new ArgumentException("Invalid argument", nameof(propertyExpression));

        var property = body.Member as PropertyInfo;

        return property == null
            ? throw new ArgumentException("Argument is not a property", nameof(propertyExpression))
            : property.Name;
    }

    private readonly ConcurrentDictionary<string, object?> propDict = [];
    protected TPropType GetVal<TPropType>(object? initValue = null, [CallerMemberName] string? propName = null)
    {
        ArgumentNullException.ThrowIfNull(propName);

        if (propDict.TryGetValue(propName, out var value))
            return (TPropType)value!;

        if (initValue != null)
        {
            propDict[propName] = initValue;
            return (TPropType)propDict[propName]!;
        }

        var initValAttr = GetType().GetProperty(propName)?.GetCustomAttribute<InitialValueAttribute>();
        propDict[propName] = initValAttr != null
            ? initValAttr.InitialValue
            : default(TPropType);
        return (TPropType)propDict[propName]!;
    }

    protected bool SetVal(object? newVal, Action? actionAfter = null, [CallerMemberName] string? propName = null)
    {
        ArgumentNullException.ThrowIfNull(propName);

        if (propDict.TryGetValue(propName, out var oldVal) && Equals(oldVal, newVal))
            return false;

        propDict[propName] = newVal;
        RaisePropertyChanged(propName);
        actionAfter?.Invoke();
        return true;
    }

    protected bool SetInitialVal(object? initVal, [CallerMemberName] string? propName = null) =>
        propName != null && propDict.TryAdd(propName, initVal);

    protected bool SetValCustomized(object? oldVal, object? newVal, Action setter, [CallerMemberName] string? propertyName = null)
    {
        if (oldVal == null && newVal == null || Equals(oldVal, newVal))
            return false;

        setter.Invoke();
        RaisePropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Копирует все public-свойства текущего объекта в destClass.
    /// </summary>
    /// <param name="destClass">Экземпляр класса того же типа, что и текущий, либо его наследник</param>
    /// <param name="silentPropertySet">Если этот параметр выставлен (по умолчанию), то в destClass непустые сеттеры свойств по возможности не вызываются
    /// (отключается внутренняя логика класса и вызовы методов SetVal())</param>
    public void CopyAllPropertiesTo(ObservableObject destClass, bool silentPropertySet = true)
    {
        ArgumentNullException.ThrowIfNull(destClass);

        var destType = destClass.GetType();
        var type = GetType();
        if (!type.IsAssignableFrom(destType))
            throw new InvalidCastException($"Object of type {destType.Name} is not assignable to type {type.Name}");

        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        props.Where(p => p.CanWrite && (!silentPropertySet || !propDict.ContainsKey(p.Name)))
            .ForEach(p => p.SetValue(destClass, p.GetValue(this)));
        if (silentPropertySet)
            propDict.ForEach(kvp => destClass.propDict[kvp.Key] = kvp.Value);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var o in propDict.Values)
            sb.Append(o == null ? "null; " : $"{o}; ");
        return sb.ToString();
    }
}
