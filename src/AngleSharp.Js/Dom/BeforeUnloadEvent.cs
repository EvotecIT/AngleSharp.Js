namespace AngleSharp.Js.Dom
{
    using AngleSharp.Attributes;
    using AngleSharp.Dom;
    using AngleSharp.Dom.Events;
    using System;
    using Jint;
    using Jint.Native;
    using Jint.Runtime;

    /// <summary>
    /// A script-host lifecycle event carrying the legacy request to cancel unloading.
    /// Hosts decide how a cancellation request affects navigation.
    /// </summary>
    [DomName("BeforeUnloadEvent")]
    public sealed class BeforeUnloadEvent : Event
    {
        /// <summary>
        /// Creates the cancelable, non-bubbling beforeunload event for a host.
        /// The interface has no JavaScript constructor.
        /// </summary>
        public BeforeUnloadEvent() : base("beforeunload", false, true)
        {
        }

        /// <summary>
        /// Gets or sets the legacy unload-cancellation message, initially empty.
        /// Assigning this value does not itself prevent the event's default action.
        /// </summary>
        [DomName("returnValue")]
        public String ReturnValue
        {
            get => _returnValue;
            set => _returnValue = value ?? "null";
        }

        private String _returnValue = String.Empty;

        /// <summary>
        /// Applies the nullable-string result of an onbeforeunload handler. Call only
        /// for that handler property, never for an addEventListener callback.
        /// </summary>
        /// <param name="ev">The event passed to the handler.</param>
        /// <param name="result">The script callback result.</param>
        public static void ApplyHandlerResult(Event ev, JsValue result)
        {
            if (!result.IsNull() && !result.IsUndefined())
            {
                var text = TypeConverter.ToString(result);
                if (ev is BeforeUnloadEvent beforeUnload && ev.Type == "beforeunload")
                {
                    ev.Cancel();
                    if (beforeUnload.ReturnValue.Length == 0)
                    {
                        beforeUnload.ReturnValue = text;
                    }
                }
            }
        }
    }
}
