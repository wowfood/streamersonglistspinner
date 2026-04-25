using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;

namespace ServerSpinner.Functions.Tests.Helpers;

public static class MockHttpRequestFactory
{
    public static (HttpRequestData Request, FunctionContext Context) Create(
        string? body = null,
        string? cookieHeader = null,
        string url = "https://example.com/api/test")
    {
        var mockContext = new Mock<FunctionContext>();
        var mockReq = new Mock<HttpRequestData>(mockContext.Object);

        var headers = new HttpHeadersCollection();
        if (cookieHeader is not null)
            headers.Add("Cookie", cookieHeader);
        mockReq.Setup(r => r.Headers).Returns(headers);

        var bodyStream = body is not null
            ? new MemoryStream(Encoding.UTF8.GetBytes(body))
            : new MemoryStream();
        mockReq.Setup(r => r.Body).Returns(bodyStream);
        mockReq.Setup(r => r.Url).Returns(new Uri(url));
        mockReq.Setup(r => r.CreateResponse())
            .Returns(() => new FakeHttpResponseData(mockContext.Object, HttpStatusCode.OK));

        return (mockReq.Object, mockContext.Object);
    }
}