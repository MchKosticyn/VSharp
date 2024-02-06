using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;


namespace VSharp.CSharpUtils;

public class AspNet
{
    [Implements("System.Threading.Tasks.Task Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerImpl.StartAsync(this, Microsoft.AspNetCore.Hosting.Server.IHttpApplication`1[TContext], System.Threading.CancellationToken)")]
    public static Task StartKestrelServer(object t, object application, object cancellationToken)
    {
        return Task.CompletedTask;
    }

    // [Implements("System.Void Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.Heartbeat.Start(this)")]
    public static void Kek(object t)
    {
    }

    // [Implements("System.Void Microsoft.AspNetCore.Hosting.HostingApplication..ctor(this, Microsoft.AspNetCore.Http.RequestDelegate, Microsoft.Extensions.Logging.ILogger, System.Diagnostics.DiagnosticListener, System.Diagnostics.ActivitySource, System.Diagnostics.DistributedContextPropagator, Microsoft.AspNetCore.Http.IHttpContextFactory)")]
    public static void CreateHostingApp(object t, RequestDelegate app, object log, object diagnosticListener, object activitySource, object distributedContextPropagator, IHttpContextFactory iHttpContextFactory)
    {
        var (path, method, body) = ("/api/get", "GET", "");

        // Generate body
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(body);
        writer.Flush();
        stream.Position = 0;

        // Create features
        var features = new FeatureCollection();

        // Create request feature
        var reqFeature = new HttpRequestFeature();
        // Get variants of paths
        reqFeature.Path = path;
        // Methods from [Get, Post, Delete, Update]
        reqFeature.Method = method;
        // Body should be symbolic
        reqFeature.Body = stream;
        features.Set<IHttpRequestFeature>(reqFeature);

        // Create response feature
        var resFeature = new HttpResponseFeature();
        resFeature.Body = new MemoryStream();
        // var resBodyFeature = new StreamResponseBodyFeature(resFeature.Body);
        // features.Set<IHttpResponseBodyFeature>(resBodyFeature);
        features.Set<IHttpResponseFeature>(resFeature);

        // Send request
        var context = iHttpContextFactory.Create(features);
        var requestTask = app.Invoke(context);
        requestTask.Wait();

        throw new AccessViolationException();
    }
}
