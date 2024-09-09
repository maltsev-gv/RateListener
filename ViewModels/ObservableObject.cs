using RateListener.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace RateListener.ViewModels
{
    /// <summary>
    /// Реализует хранение значений свойств и быстрый доступ к ним из дочерних классов. Реализует INotifyPropertyChanged. Изменение свойства инициирует вызов PropertyChanged
    /// </summary>
    [Serializable]
    public class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected PropertyChangedEventHandler PropertyChangedHandler => PropertyChanged;

        public event PropertyChangingEventHandler PropertyChanging;
        protected PropertyChangingEventHandler PropertyChangingHandler => PropertyChanging;

        public void VerifyPropertyName([CallerMemberName] string propertyName = null)
        {
            var myType = GetType();
            if (!string.IsNullOrEmpty(propertyName) && myType.GetProperty(propertyName) == null)
            {
                throw new ArgumentException("Property not found", propertyName);
            }
        }

        protected virtual void RaisePropertyChanging([CallerMemberName] string propertyName = null)
        {
#if DEBUG
            VerifyPropertyName(propertyName);
#endif
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        protected virtual void RaisePropertyChanging<T>(Expression<Func<T>> propertyExpression)
        {
            var propertyName = GetPropertyName(propertyExpression);
            RaisePropertyChanging(propertyName);
        }

        protected bool IsTraceEnabled = false;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged == null)
            {
                return;
            }

            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            if (!string.IsNullOrEmpty(propertyName) && !_propDict.ContainsKey(propertyName))
            {
                var pi = GetType().GetProperty(propertyName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                if (pi != null)
                {
                    _propDict[propertyName] = pi.GetValue(this, null);
                }
                else
                {
                    throw new ArgumentException("Property not found", propertyName);
                }
            }
            if (IsTraceEnabled)
            {
                if (!string.IsNullOrEmpty(propertyName))
                {
                    object propVal = _propDict[propertyName];
                    string strVal = propVal == null
                        ? "null"
                        : $"{propVal}, hash = {propVal.GetHashCode()}";
                    Console.WriteLine($"RaisePropertyChanged: {propertyName} is now \"{strVal}\"");
                }
                else
                {
                    Console.WriteLine($"RaisePropertyChanged: All properties updated");
                }
            }
        }

        protected virtual void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
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
            if (propertyExpression == null)
            {
                throw new ArgumentNullException("propertyExpression");
            }

            var body = propertyExpression.Body as MemberExpression;

            if (body == null)
            {
                throw new ArgumentException("Invalid argument", "propertyExpression");
            }

            var property = body.Member as PropertyInfo;

            if (property == null)
            {
                throw new ArgumentException("Argument is not a property", "propertyExpression");
            }

            return property.Name;
        }

        private readonly Dictionary<string, object> _propDict = new Dictionary<string, object>();
        protected PropType GetVal<PropType>(object initValue = null, [CallerMemberName] string propName = null)
        {
            if (_propDict.ContainsKey(propName))
            {
                return (PropType)_propDict[propName];
            }

            if (initValue != null)
            {
                _propDict[propName] = initValue;
                return (PropType)_propDict[propName];
            }

            var initValAttr = GetType().GetProperty(propName)?.GetCustomAttribute<InitialValueAttribute>();
            if (initValAttr != null)
            {
                _propDict[propName] = initValAttr.InitialValue;
            }
            else
            {
                _propDict[propName] = default(PropType);
            }
            return (PropType)_propDict[propName];
        }

        protected bool SetVal(object newVal, Action actionAfter = null, [CallerMemberName] string propName = null)
        {
            if (_propDict.ContainsKey(propName) && _propDict[propName] != null && _propDict[propName].Equals(newVal))
            {
                return false;
            }
            _propDict[propName] = newVal;
            RaisePropertyChanged(propName);
            actionAfter?.Invoke();
            return true;
        }

        protected bool SetInitialVal(object initVal, [CallerMemberName] string propName = null)
        {
            if (_propDict.ContainsKey(propName))
            {
                return false;
            }
            _propDict[propName] = initVal;
            return true;
        }

        protected bool SetValCustomized(object oldVal, object newVal, Action setter, [CallerMemberName] string propertyName = null)
        {
            if (oldVal == null && newVal == null)
            {
                return false;
            }
            if (!Equals(oldVal, newVal))
            {
                setter.Invoke();
                RaisePropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Копирует все public-свойства текущего объекта в destClass. 
        /// </summary>
        /// <param name="destClass">экземпляр класса того же типа, что и текущий, либо его наследник</param>
        /// <param name="silentPropertySet">если этот параметр выставлен (по умолчанию), то в destClass непустые сеттеры свойств по возможности не вызываются
        /// (отключается внутренняя логика класса и вызовы методов SetVal())</param>
        public void CopyAllPropertiesTo(ObservableObject destClass, bool silentPropertySet = true)
        {
            var destType = destClass.GetType();
            var type = GetType();
            if (!type.IsAssignableFrom(destType))
            {
                throw new InvalidCastException($"Object of type {destType.Name} is not assignable to type {type.Name}");
            }

            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            props.Where(p => p.CanWrite && (!silentPropertySet || !_propDict.ContainsKey(p.Name)))
                .ForEach(p => p.SetValue(destClass, p.GetValue(this)));
            if (silentPropertySet)
            {
                _propDict.ForEach(kvp => destClass._propDict[kvp.Key] = kvp.Value);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var o in _propDict.Values)
            {
                sb.Append(o == null ? "null; " : $"{o}; ");
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Присваивает начальное значение свойству класса - наследника ObservableObject
    /// </summary>
    public class InitialValueAttribute : Attribute
    {
        public InitialValueAttribute(object value)
        {
            InitialValue = value;
        }

        public object InitialValue { get; }
    }
}
