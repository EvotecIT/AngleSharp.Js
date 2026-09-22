# Runtime integration fork

This EvotecIT fork builds on AngleSharp.Js v1.1.0 and retains its upstream source,
license, and Jint dependency. Jint itself is unmodified.

`JsScriptingOptions.ConfigureEngine` configures Jint options before a window's
engine is constructed. The service snapshots the callback along with the other
options. DOM enum constants are exposed on their corresponding constructors and
prototypes. DOM nodes preserve their identity through `SameObject` properties.
NodeList iterable methods live on its prototype, and HTMLCollection retains its
separate collection contract.

The canonical identity correction was merged upstream in
[AngleSharp.Js #141](https://github.com/AngleSharp/AngleSharp.Js/pull/141).

Module loading, import maps, resource policy, and runtime orchestration belong to
OfficeIMO. They do not belong in this binding layer.

The current integration branch uses `EventTarget.EventListenerRemoved` to clear
script handler properties after native removal. Build it with the matching
AngleSharp fork source. A sibling `AngleSharp` checkout is selected automatically;
an isolated checkout needs an explicit source project:

```sh
dotnet build src/AngleSharp.Js/AngleSharp.Js.csproj -f net10.0 \
  -p:AngleSharpTestProject=/path/to/AngleSharp/src/AngleSharp/AngleSharp.Core.csproj
```

Qualify the combined binding tests below and the consuming runtime before updating
an OfficeIMO revision pin. Fork builds are not official AngleSharp.Js releases.
Do not publish packages under upstream package identities.

Document availability follows `readystatechange` rather than relying on a document
`load` event. Subscription and completion checks run on the document event loop,
and cancellation applies to document creation, readiness, and stabilization waits.

`DOMStringMap` has a dedicated live named-property binding: enumerable data
properties, deletion, inherited-name overrides, JavaScript value conversion and
DOM exception translation. Symbols remain ordinary properties. Camel-case conversion and invalid attribute names require the AngleSharp core
fork and are also qualified by OfficeIMO's `RuntimeDatasetTests`.

`BeforeUnloadEvent` supplies the legacy string `returnValue` and the specialized
nullable-string result contract for `onbeforeunload`. Body properties and inline
attributes share the window handler; replacing a handler keeps its listener order.
Navigation decisions and confirmation UI belong to the host.

The native window subscription requires the AngleSharp core correction that uses
`EventNames.BeforeUnload`, tracked in [AngleSharp #1354](https://github.com/AngleSharp/AngleSharp/pull/1354).
Qualify this combined contract with the
core fork source, including when its correction is not in the official package:

```sh
dotnet test src/AngleSharp.Js.Tests/AngleSharp.Js.Tests.csproj -f net10.0 \
  -p:AngleSharpTestProject=/path/to/AngleSharp/src/AngleSharp/AngleSharp.Core.csproj
```

Without that source reference, the configured official AngleSharp 1.8.0 package
lacks `EventTarget.EventListenerRemoved`, so this integration branch cannot build
standalone or publish a usable package against that dependency. Its package lane
must be qualified after a compatible AngleSharp release or an explicitly pinned
source-based fork package; do not represent the current source build as a package
qualification.

HTML `DOMParser` documents use an inert context without the active script engine,
event loop, or inline-handler observers. Worker realm setup parses its backing
document synchronously instead of waiting for navigation lifecycle tasks on its
own loop. Body `onload` handlers target the window, matching the core fork's load
lifecycle. These paths are included in the combined provider test command above.
