using System.Text.Json.Serialization;
using WhaleeBot.Services;

namespace WhaleeBot.Models.NapcatRequests;

[JsonDerivedType(typeof(SendPrivateMessage), "send_private_msg")]
public abstract class NapcatRequestBase
{
}