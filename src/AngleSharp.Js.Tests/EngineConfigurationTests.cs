namespace AngleSharp.Js.Tests
{
    using AngleSharp.Dom;
    using AngleSharp.Scripting;
    using Jint;
    using NUnit.Framework;
    using System.Threading.Tasks;

    [TestFixture]
    public class EngineConfigurationTests
    {
        [Test]
        public async Task ConfigurationRunsOnceForTheWindowAndUsesTheOptionsSnapshot()
        {
            var calls = 0;
            IWindow configuredWindow = null;
            var options = new JsScriptingOptions
            {
                ConfigureEngine = (window, engine) =>
                {
                    calls++;
                    configuredWindow = window;
                    engine.Strict();
                }
            };
            var configuration = Configuration.Default.WithJs(options);
            options.ConfigureEngine = (_, __) => Assert.Fail("The caller's later mutation must not replace the callback.");
            using var context = BrowsingContext.New(configuration);
            var document = await context.OpenAsync(response => response.Content("<p>test</p>")).ConfigureAwait(false);
            var service = context.GetService<JsScriptingService>();
            var engine = service.GetOrCreateJint(document);

            Assert.AreSame(document.DefaultView, configuredWindow);
            Assert.AreSame(engine, service.GetOrCreateJint(document));
            Assert.AreEqual(1, calls);
            Assert.AreEqual("ReferenceError", engine.Evaluate("try { undeclaredValue = 1; 'no error' } catch (e) { e.name }").ToString());
        }
    }
}
