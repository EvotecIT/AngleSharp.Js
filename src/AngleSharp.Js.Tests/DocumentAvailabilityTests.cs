namespace AngleSharp.Js.Tests
{
    using AngleSharp.Dom;
    using AngleSharp.Browser;
    using NUnit.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class DocumentAvailabilityTests
    {
        [Test]
        public async Task CompletionDoesNotRequireDeliveryOfADocumentLoadEvent()
        {
            using var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader(new Io.LoaderOptions { IsResourceLoadingEnabled = true }));
            using var child = context.CreateChild("test", Sandboxes.None);
            var document = await child.OpenNewAsync();
            document.Open();
            Assert.AreEqual(DocumentReadyState.Loading, document.ReadyState);
            document.AddEventListener("load", (_, ev) => ev.StopImmediately());
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var available = document.WaitUntilAvailable(deadline.Token);
            document.Close();
            Assert.AreEqual(DocumentReadyState.Complete, document.ReadyState);
            Assert.AreSame(available, await Task.WhenAny(available, Task.Delay(2000)));
            Assert.AreSame(document, await available);
        }

        [Test]
        public async Task CancellationIsReportedWhileWaitingForReadiness()
        {
            using var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader(new Io.LoaderOptions { IsResourceLoadingEnabled = true }));
            using var child = context.CreateChild("test", Sandboxes.None);
            var document = await child.OpenNewAsync();
            document.Open();
            using var cancellation = new CancellationTokenSource();
            var available = document.WaitUntilAvailable(cancellation.Token);
            cancellation.Cancel();
            Assert.AreSame(available, await Task.WhenAny(available, Task.Delay(2000)));
            Assert.ThrowsAsync<TaskCanceledException>(async () => await available);
        }

        [Test]
        public async Task CancellationIsReportedWhileWaitingForTheDocumentTask()
        {
            var pending = new TaskCompletionSource<IDocument>();
            using var cancellation = new CancellationTokenSource();
            var available = pending.Task.WaitUntilAvailable(cancellation.Token);
            cancellation.Cancel();
            Assert.AreSame(available, await Task.WhenAny(available, Task.Delay(2000)));
            Assert.ThrowsAsync<TaskCanceledException>(async () => await available);
        }
    }
}
