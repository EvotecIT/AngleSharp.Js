# Runtime integration fork

This EvotecIT fork builds on AngleSharp.Js v1.1.0 and retains its upstream source,
license, and Jint dependency. Jint itself is unmodified.

`JsScriptingOptions.ConfigureEngine` configures Jint options before a window's
engine is constructed. The service snapshots the callback along with the other
options. DOM enum constants are exposed on their corresponding constructors and
prototypes. NodeList bindings use the current upstream implementation.

Module loading, import maps, resource policy, and runtime orchestration belong to
OfficeIMO. They do not belong in this binding layer.

```sh
dotnet build src/AngleSharp.Js/AngleSharp.Js.csproj -f net10.0
dotnet test src/AngleSharp.Js.Tests/AngleSharp.Js.Tests.csproj -f net10.0
```

Qualify the standalone binding tests and the consuming runtime before updating an
OfficeIMO revision pin. Fork builds are not official AngleSharp.Js releases.
Do not publish packages under upstream package identities.
