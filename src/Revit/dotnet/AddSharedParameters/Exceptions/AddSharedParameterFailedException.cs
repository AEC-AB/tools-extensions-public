using System.Runtime.Serialization;

namespace AddSharedParameters.Exceptions;

[Serializable]
internal class AddSharedParameterFailedException : Exception
{
    public AddSharedParameterFailedException()
    {
    }

    public AddSharedParameterFailedException(string message) : base(message)
    {
    }

    public AddSharedParameterFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected AddSharedParameterFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
