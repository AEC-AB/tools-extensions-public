using System.Runtime.Serialization;

namespace AddSharedParameters.Exceptions;

[Serializable]
internal class AddSharedParameterPartiallySucceededException : Exception
{
    public AddSharedParameterPartiallySucceededException()
    {
    }

    public AddSharedParameterPartiallySucceededException(string message) : base(message)
    {
    }

    public AddSharedParameterPartiallySucceededException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected AddSharedParameterPartiallySucceededException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}