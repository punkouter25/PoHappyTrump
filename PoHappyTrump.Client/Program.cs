using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http;
using Radzen; // Add this line
using Radzen.Blazor; // Add this line

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
