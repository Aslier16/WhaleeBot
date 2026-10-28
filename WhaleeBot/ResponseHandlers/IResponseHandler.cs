using WhaleeBot.Models.NapcatRequests;
using WhaleeBot.Services;

namespace WhaleeBot.ResponseHandlers;

public interface IResponseHandler
{
    Type HandledRequestType { get; }
    
    /// <summary>
    /// The description given to the llm for this component
    /// </summary>
    string Description { get; }
    /// <summary>
    /// The format of llm's json response
    /// </summary>
    string Format { get; }
    /// <summary>
    /// The path to send the request to.
    /// Start with '/'
    /// </summary>
    string Path { get; }
    
    Task ProcessAsync(IChatRouter chatRouter,NapcatRequestBase request, CancellationToken ctx);
}

public interface IResponseHandler<T> : IResponseHandler where T : NapcatRequestBase
{
    Type IResponseHandler.HandledRequestType => typeof(T);

    Task IResponseHandler.ProcessAsync(IChatRouter chatRouter, NapcatRequestBase request, CancellationToken ctx) => ProcessAsync(chatRouter, (T)request, ctx);

    Task ProcessAsync(IChatRouter chatRouter,T request, CancellationToken ctx);
}