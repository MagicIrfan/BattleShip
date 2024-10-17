using System.Net;

namespace BattleShip.Exceptions;

public class AttackException : Exception
{
    public AttackException(string message, HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}