using System.IO;
using Autodesk.Revit.ApplicationServices;

namespace AddSharedParameters.Helpers;

internal class SharedParameterFileHandler : IDisposable
{
    private readonly Application _application;
    private string? _initialSharedParameterFileName;

    internal SharedParameterFileHandler(Application application)
    {
        _application = application;
    }

    internal DefinitionFile OpenSharedParameterFile(string path)
    {
        _initialSharedParameterFileName = _application.SharedParametersFilename;

        if (string.IsNullOrEmpty(path))
            throw new Exception("Shared parameter file path is empty");

        if (!File.Exists(path))
            throw new Exception($"Could not find shared parameter file: {path}");

        _application.SharedParametersFilename = path;
        return _application.OpenSharedParameterFile();
    }

    private void ResetActiveSharedParameter()
    {
        _application.SharedParametersFilename = _initialSharedParameterFileName;
        _application.OpenSharedParameterFile();
    }

    public void Dispose()
    {
        ResetActiveSharedParameter();
    }
}
