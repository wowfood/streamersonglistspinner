using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ServerSpinner.Functions.Tests.Helpers;

public class FakeHttpResponseData : HttpResponseData
{
    public FakeHttpResponseData(FunctionContext context, HttpStatusCode statusCode)
        : base(context)
    {
        StatusCode = statusCode;
        Headers = new HttpHeadersCollection();
        Body = new MemoryStream();
    }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; }
    public override Stream Body { get; set; }
    public override HttpCookies Cookies => throw new NotImplementedException();

    public string GetBodyAsString()
    {
        Body.Position = 0;
        using var reader = new StreamReader(Body, leaveOpen: true);
        return reader.ReadToEnd();
    }
}