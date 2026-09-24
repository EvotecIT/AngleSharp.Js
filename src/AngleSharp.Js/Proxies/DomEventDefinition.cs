namespace AngleSharp.Js
{
    using AngleSharp.Dom;
    using AngleSharp.Html.Dom;
    using AngleSharp.Js.Dom;
    using Jint;
    using Jint.Native;
    using Jint.Native.Function;
    using System;
    using System.Reflection;

    /// <summary>
    /// One event-handler member of a DOM type - "onclick" and its kin - as the accessor pair
    /// a prototype declares for it.
    /// </summary>
    /// <remarks>
    /// The pair is process-shared along with the rest of the type's members, so this object
    /// must stay free of anything belonging to an engine: it holds the two reflected accessors
    /// and derives everything else from the receiver. It doubles as the key a node files its
    /// handler under, which is what keeps the handler itself - the one thing here that is per
    /// node and per engine - on the node.
    /// </remarks>
    sealed class DomEventDefinition
    {
        private readonly String _name;
        private readonly MethodInfo _addHandler;
        private readonly MethodInfo _removeHandler;

        public DomEventDefinition(String name, MethodInfo addHandler, MethodInfo removeHandler)
        {
            _name = name;
            _addHandler = addHandler;
            _removeHandler = removeHandler;
        }

        private static DomNodeInstance GetNode(JsValue receiver)
        {
            if (receiver is DomNodeInstance node)
            {
                return node;
            }

            var instance = receiver.GetEngineInstance();
            return instance != null && ReferenceEquals(receiver, instance.Jint.Global)
                ? instance.Window
                : null;
        }

        public JsValue GetHandler(JsValue thisObject, JsValue[] arguments)
        {
            var node = GetNode(thisObject);
            if ((_name == "onbeforeunload" || _name == "onload") && node?.Value is IHtmlBodyElement body)
            {
                return body.Owner != null && ReferenceEquals(body.Owner.Context.Active, body.Owner)
                    ? node.Instance.GetDomNode(body.Owner.DefaultView).Get(_name)
                    : JsValue.Null;
            }
            var registration = node?.GetEventHandler(this);
            return registration?.Function ?? JsValue.Null;
        }

        public JsValue SetHandler(JsValue thisObject, JsValue[] arguments)
        {
            var node = GetNode(thisObject);
            var value = arguments.Length > 0 ? arguments[0] : JsValue.Undefined;

            if ((_name == "onbeforeunload" || _name == "onload") && node?.Value is IHtmlBodyElement body)
            {
                if (body.Owner != null && ReferenceEquals(body.Owner.Context.Active, body.Owner))
                {
                    node.Instance.GetDomNode(body.Owner.DefaultView).Set(_name, value);
                }
                return value;
            }

            if (node != null)
            {
                var previous = node.GetEventHandler(this);
                if (previous != null && value is Function replacement)
                {
                    previous.Function = replacement;
                    return value;
                }
                node.RemoveEventHandler(this);

                if (previous != null)
                {
                    _removeHandler?.Invoke(node.Value, new Object[] { previous.Handler });
                }

                if (value is Function function)
                {
                    var engine = node.Instance;

                    Registration registration = null;
                    DomEventHandler handler = (s, ev) =>
                    {
                        var sender = s.ToJsValue(engine);
                        var args = ev.ToJsValue(engine);
                        var result = registration.Function.Call(sender, new[] { args });
                        if (_name == "onbeforeunload")
                        {
                            BeforeUnloadEvent.ApplyHandlerResult(ev, result);
                        }
                    };

                    registration = new Registration(function, handler);
                    node.SetEventHandler(this, registration);
                    _addHandler?.Invoke(node.Value, new Object[] { handler });
                }
            }

            return value;
        }

        /// <summary>
        /// The handler currently assigned to a single node for a single event.
        /// </summary>
        public sealed class Registration
        {
            public Registration(Function function, DomEventHandler handler)
            {
                Function = function;
                Handler = handler;
            }

            /// <summary>
            /// The function that was assigned, as it has to be handed back on read.
            /// </summary>
            public Function Function { get; set; }

            /// <summary>
            /// The listener that was subscribed, as it has to be handed back on removal.
            /// </summary>
            public DomEventHandler Handler { get; }

        }
    }
}
