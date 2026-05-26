using System;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;
using FluentFTP.Exceptions;

namespace StreamBIMDownloader;

internal static class StreamBimExceptionHelper
{
    internal static Exception GetInnermostException(Exception exception)
    {
        while (exception.InnerException is not null)
        {
            exception = exception.InnerException;
        }

        return exception;
    }

    internal static string GetInnermostMessage(Exception exception)
    {
        return GetInnermostException(exception).Message;
    }

    internal static bool IsHandledFailure(Exception exception)
    {
        return exception is ArgumentException
            or InvalidOperationException
            or UnauthorizedAccessException
            or FtpException
            or IOException
            or SocketException
            or TimeoutException
            or AuthenticationException
            or Win32Exception;
    }

    internal static bool IsTransientFtpFailure(Exception exception)
    {
        return exception is FtpException
            or IOException
            or SocketException
            or TimeoutException
            or AuthenticationException;
    }
}