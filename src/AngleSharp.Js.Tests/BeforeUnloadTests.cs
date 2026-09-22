namespace AngleSharp.Js.Tests
{
    using AngleSharp.Dom;
    using AngleSharp.Dom.Events;
    using AngleSharp.Js.Dom;
    using AngleSharp.Scripting;
    using Jint;
    using NUnit.Framework;
    using System.Threading.Tasks;

    [TestFixture]
    public class BeforeUnloadTests
    {
        [TestCase("'unsaved'", true, "unsaved")]
        [TestCase("''", true, "")]
        [TestCase("false", true, "false")]
        [TestCase("0", true, "0")]
        [TestCase("null", false, "")]
        [TestCase("undefined", false, "")]
        [TestCase("({toString(){return 'converted'}})", true, "converted")]
        public async Task HandlerUsesNullableStringReturnContract(string expression, bool canceled, string message)
        {
            using var context = BrowsingContext.New(Configuration.Default.WithJs());
            var document = await context.OpenAsync(r => r.Content("<body></body>")).ConfigureAwait(false);
            var engine = context.GetService<JsScriptingService>().GetOrCreateJint(document);
            engine.Execute("window.called=0; window.onbeforeunload=()=>{window.called++;return " + expression + ";}");
            Assert.IsTrue(engine.Evaluate("typeof window.onbeforeunload === 'function'").AsBoolean());
            var ev = new BeforeUnloadEvent();
            document.DefaultView.Dispatch(ev);
            Assert.AreEqual(1, engine.Evaluate("window.called").AsNumber(), "handler called");
            Assert.AreEqual(canceled, ev.IsDefaultPrevented);
            Assert.AreEqual(message, ev.ReturnValue);
        }

        [Test]
        public async Task ListenerReturnIsIgnoredAndExistingMessageIsPreserved()
        {
            using var context = BrowsingContext.New(Configuration.Default.WithJs());
            var document = await context.OpenAsync(r => r.Content("<body></body>")).ConfigureAwait(false);
            var engine = context.GetService<JsScriptingService>().GetOrCreateJint(document);
            engine.Execute("addEventListener('beforeunload',()=> 'ignored')");
            var ev = new BeforeUnloadEvent();
            document.DefaultView.Dispatch(ev);
            Assert.IsFalse(ev.IsDefaultPrevented);
            Assert.AreEqual("", ev.ReturnValue);
            engine.Execute("window.onbeforeunload=event=>{event.returnValue='first';return 'second'}");
            ev = new BeforeUnloadEvent();
            document.DefaultView.Dispatch(ev);
            Assert.IsTrue(ev.IsDefaultPrevented);
            Assert.AreEqual("first", ev.ReturnValue);
        }

        [TestCase("null", "null")]
        [TestCase("undefined", "undefined")]
        [TestCase("({toString(){return 'converted'}})", "converted")]
        public async Task LegacyReturnValueUsesDomStringConversion(string expression, string expected)
        {
            using var context = BrowsingContext.New(Configuration.Default.WithJs());
            var document = await context.OpenAsync(r => r.Content("<body></body>")).ConfigureAwait(false);
            var engine = context.GetService<JsScriptingService>().GetOrCreateJint(document);
            var ev = new BeforeUnloadEvent();
            engine.SetValue("unloadEvent", ev);
            engine.Execute("unloadEvent.returnValue=" + expression);
            Assert.AreEqual(expected, ev.ReturnValue);
            Assert.IsFalse(ev.IsDefaultPrevented);
            Assert.IsTrue(engine.Evaluate("unloadEvent instanceof BeforeUnloadEvent && unloadEvent instanceof Event").AsBoolean());
        }

        [Test]
        public async Task BodyAndInlineHandlersShareWindowSlotAndReplacementOrder()
        {
            using var context = BrowsingContext.New(Configuration.Default.WithJs());
            var document = await context.OpenAsync(r => r.Content("<body></body>")).ConfigureAwait(false);
            var engine = context.GetService<JsScriptingService>().GetOrCreateJint(document);
            engine.Execute(@"window.order=[];
                onbeforeunload=()=>{window.order.push('old');return 'old'};
                window.addEventListener('beforeunload',()=>window.order.push('listener'));
                document.body.onbeforeunload=()=>{window.order.push('new');return 'new'};");
            var ev = new BeforeUnloadEvent();
            document.DefaultView.Dispatch(ev);
            Assert.AreEqual("new,listener", engine.Evaluate("window.order.join(',')").ToString());
            Assert.AreEqual("new", ev.ReturnValue);
            Assert.IsTrue(engine.Evaluate("document.body.onbeforeunload===window.onbeforeunload").AsBoolean());
            engine.Execute("document.body.setAttribute('onbeforeunload', \"return 'inline'\")");
            ev = new BeforeUnloadEvent();
            document.DefaultView.Dispatch(ev);
            Assert.AreEqual("inline", ev.ReturnValue);
            engine.Execute("document.body.removeAttribute('onbeforeunload')");
            ev = new BeforeUnloadEvent();
            document.DefaultView.Dispatch(ev);
            Assert.IsFalse(ev.IsDefaultPrevented);
            Assert.IsTrue(engine.Evaluate("window.onbeforeunload===null").AsBoolean());
        }

        [Test]
        public async Task SyntheticOrdinaryEventDoesNotAcquireBeforeUnloadCancellation()
        {
            using var context = BrowsingContext.New(Configuration.Default.WithJs());
            var document = await context.OpenAsync(r => r.Content("<body></body>")).ConfigureAwait(false);
            var engine = context.GetService<JsScriptingService>().GetOrCreateJint(document);
            engine.Execute("window.onbeforeunload=()=>false");
            var ev = new Event("beforeunload", false, true);
            document.DefaultView.Dispatch(ev);
            Assert.IsFalse(ev.IsDefaultPrevented);
            Assert.AreEqual("TypeError", engine.Evaluate("try { new BeforeUnloadEvent(); } catch(e) { e.name }").ToString());
        }
    }
}
