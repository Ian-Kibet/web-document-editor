using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using DocumentEditor.Engine.Interop;
using DocumentEditor.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton<EditorEngine>();

var host = builder.Build();

var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
var engine = host.Services.GetRequiredService<EditorEngine>();
var engineRef = DotNetObjectReference.Create(new JsBridge(engine));
await jsRuntime.InvokeVoidAsync("setDotNetReference", engineRef);

await host.RunAsync();
