// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Registry;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;

namespace Bicep.Wasm;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddSingleton<IFileSystem, MockFileSystem>();
        builder.Services.AddSingleton<IModuleRegistryProvider, EmptyModuleRegistryProvider>();
        builder.Services.AddBicepCore();
        builder.Services.AddBicepDecompiler();

        var serviceProvider = builder.Services.BuildServiceProvider();

        var jsRuntime = serviceProvider.GetRequiredService<IJSRuntime>();
        var interop = new Interop(jsRuntime, serviceProvider);
        await jsRuntime.InvokeAsync<object>("BicepInitialize", DotNetObjectReference.Create(interop));

        await builder.Build().RunAsync();
    }
}
