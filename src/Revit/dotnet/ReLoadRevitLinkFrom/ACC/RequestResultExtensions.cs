using System;
using CW.Assistant.Extensions;

namespace ReLoadRevitLinkFrom.ACC;

public static class RequestResultExtensions
{
    public static T GetResult<T>(this RequestResult<T> response)
    {
        if ((int)response.StatusCode > 299)
            throw new Exception(response.ReasonPhrase);
        return response.Result ?? throw new Exception("Result is null");
    }

}
