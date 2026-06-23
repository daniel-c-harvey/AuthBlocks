using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Mirror the consumer WASM client: register the AuthBlocks client-side auth
// services (AuthorizationCore + cascading auth state + state deserialization)
// so the InteractiveAuto/WASM render mode composes the same way it does in
// a real deployment.
AuthBlocksWeb.Client.Startup.ConfigureServices(builder.Services);

await builder.Build().RunAsync();
