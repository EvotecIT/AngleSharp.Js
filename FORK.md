# Runtime integration fork

This EvotecIT fork builds on AngleSharp.Js v1.1.0 and retains its upstream source,
license, and Jint dependency. Jint itself is unmodified.

`JsScriptingOptions.ConfigureEngine` configures Jint options before a window's
engine is constructed. The service snapshots the callback along with the other
options. DOM enum constants are exposed on their corresponding constructors and
prototypes. DOM nodes preserve their identity through `SameObject` properties.
NodeList iterable methods live on its prototype, and HTMLCollection retains its
separate collection contract.

Module loading, import maps, resource policy, and runtime orchestration belong to
OfficeIMO. They do not belong in this binding layer.

```sh
dotnet build src/AngleSharp.Js/AngleSharp.Js.csproj -f net10.0
dotnet test src/AngleSharp.Js.Tests/AngleSharp.Js.Tests.csproj -f net10.0
```

Qualify the standalone binding tests and the consuming runtime before updating an
OfficeIMO revision pin. Fork builds are not official AngleSharp.Js releases.
Do not publish packages under upstream package identities.

Document availability follows `readystatechange` rather than relying on a document
`load` event. Subscription and completion checks run on the document event loop,
and cancellation applies to document creation, readiness, and stabilization waits.

`DOMStringMap` has a dedicated live named-property binding: enumerable data
properties, deletion, inherited-name overrides, JavaScript value conversion and
DOM exception translation. Symbols remain ordinary properties. The standalone
suite qualifies binding behavior against the official AngleSharp dependency;
camel-case conversion and invalid attribute names additionally require the
AngleSharp core fork and are qualified by OfficeIMO's `RuntimeDatasetTests`.

`BeforeUnloadEvent` supplies the legacy string `returnValue` and the specialized
nullable-string result contract for `onbeforeunload`. Body properties and inline
attributes share the window handler; replacing a handler keeps its listener order.
Navigation decisions and confirmation UI belong to the host.

The native window subscription requires the AngleSharp core correction that maps
`EventNames.Unloading` to `beforeunload`. Qualify this combined contract with the
core fork source, including when its correction is not in the official package:

```sh
dotnet test src/AngleSharp.Js.Tests/AngleSharp.Js.Tests.csproj -f net10.0 \
  -p:AngleSharpTestProject=/path/to/AngleSharp/src/AngleSharp/AngleSharp.Core.csproj
```

Without that source reference, the configured official AngleSharp 1.8.0 package's event-name defect
still fails the new window `beforeunload` integration tests. The JS library itself
continues to compile against the official core API; no host workaround changes
the native subscription name.

HTML `DOMParser` documents use an inert context without the active script engine,
event loop, or inline-handler observers. Worker realm setup parses its backing
document synchronously instead of waiting for navigation lifecycle tasks on its
own loop. Body `onload` handlers target the window, matching the core fork's load
lifecycle. These paths are included in the combined provider test command above.
