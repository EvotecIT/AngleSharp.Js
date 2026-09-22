namespace AngleSharp.Js
{
    using AngleSharp.Dom;
    using Jint;
    using Jint.Native;
    using Jint.Native.Object;
    using Jint.Runtime;
    using Jint.Runtime.Descriptors;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Projects a live DOMStringMap as enumerable named properties. Dataset names override
    /// inherited members; symbols remain ordinary JavaScript properties.
    /// </summary>
    sealed class DomStringMapInstance : ObjectInstance, IDomProxy
    {
        private IStringMap _map;

        public DomStringMapInstance(EngineInstance instance, IStringMap map, Type type)
            : base(instance.Jint)
        {
            Instance = instance;
            DomType = type;
            _map = map;
            Prototype = instance.GetDomPrototype(type);
        }

        public Object Value => _map;
        public Type DomType { get; }
        public EngineInstance Instance { get; }
        public void Update(Object value) => _map = (IStringMap)value;
        public override Object ToObject() => _map;

        public override PropertyDescriptor GetOwnProperty(JsValue property)
        {
            if (!property.IsSymbol() && _map[property.ToString()] is String value)
            {
                return new PropertyDescriptor(value, true, true, true);
            }

            return base.GetOwnProperty(property);
        }

        public override List<JsValue> GetOwnPropertyKeys(Types types = Types.String | Types.Symbol)
        {
            var keys = new List<JsValue>();
            if ((types & Types.String) != 0)
            {
                foreach (var entry in _map)
                {
                    keys.Add(entry.Key);
                }
            }

            keys.AddRange(base.GetOwnPropertyKeys(types));
            return keys;
        }

        public override IEnumerable<KeyValuePair<JsValue, PropertyDescriptor>> GetOwnProperties()
        {
            foreach (var key in GetOwnPropertyKeys())
            {
                yield return new KeyValuePair<JsValue, PropertyDescriptor>(key, GetOwnProperty(key));
            }
        }

        public override Boolean Set(JsValue property, JsValue value, JsValue receiver)
        {
            if (property.IsSymbol())
            {
                return base.Set(property, value, receiver);
            }

            if (ReferenceEquals(receiver, this))
            {
                SetNamedValue(property, value);
                return true;
            }

            // A distinct receiver uses ordinary lookup with named properties ignored.
            // String properties live in the DOM map, so continue at the prototype.
            if (Prototype != null)
            {
                return Prototype.Set(property, value, receiver);
            }

            if (receiver is ObjectInstance target)
            {
                var descriptor = target.GetOwnProperty(property);
                if (descriptor == PropertyDescriptor.Undefined)
                {
                    return target.CreateDataProperty(property, value);
                }

                if (descriptor.IsDataDescriptor() && descriptor.Writable)
                {
                    return target.DefineOwnProperty(property, new PropertyDescriptor(value, PropertyFlag.None));
                }
            }

            return false;
        }

        public override Boolean DefineOwnProperty(JsValue property, PropertyDescriptor descriptor)
        {
            if (!property.IsSymbol())
            {
                if (!descriptor.IsDataDescriptor())
                {
                    return false;
                }

                SetNamedValue(property, descriptor.Value ?? JsValue.Undefined);
                return true;
            }

            return base.DefineOwnProperty(property, descriptor);
        }

        public override Boolean Delete(JsValue property)
        {
            // Unsupported names must not alias a supported camel-case name in the deleter.
            if (!property.IsSymbol() && _map[property.ToString()] != null)
            {
                _map.Remove(property.ToString());
                return true;
            }

            return base.Delete(property);
        }

        // Web IDL legacy platform objects remain extensible as their backing DOM changes.
        public override Boolean PreventExtensions() => false;

        private void SetNamedValue(JsValue property, JsValue value)
        {
            var text = TypeConverter.ToString(value);
            try
            {
                _map[property.ToString()] = text;
            }
            catch (DomException error)
            {
                var exception = Instance.GetDomNode(error);
                var name = error.Code == (Int32)DomError.Syntax ? "SyntaxError" :
                    error.Code == (Int32)DomError.InvalidCharacter ? "InvalidCharacterError" : error.Name;
                exception.FastSetDataProperty("name", name);
                exception.FastSetDataProperty("message", error.Message);
                throw new JavaScriptException(exception);
            }
        }
    }
}
