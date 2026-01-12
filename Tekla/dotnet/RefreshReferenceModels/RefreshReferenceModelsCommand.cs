namespace RefreshReferenceModels;

public class RefreshReferenceModelsCommand : ITeklaExtension<RefreshReferenceModelsArgs>
{
    public IExtensionResult Run(ITeklaExtensionContext context, RefreshReferenceModelsArgs args, CancellationToken cancellationToken)
    {
        var result = new RefreshReferenceModelsResult { Result = ExecutionResult.Succeeded };
            var model = new Model();
            var selectObjects = model.GetModelObjectSelector().GetAllObjectsWithType(ModelObject.ModelObjectEnum.REFERENCE_MODEL);
            var refrenceModels = new List<ReferenceModel>();
            while (selectObjects.MoveNext())
            {
                var modelObject = selectObjects.Current as ReferenceModel;
                if (modelObject != null)
                {
                    refrenceModels.Add(modelObject);
                }
            }

            var refreshStatus = new List<(bool, string)>();

            foreach (var refrenceModel in refrenceModels)
            {
                var succeeded = refrenceModel.RefreshFile();
                refreshStatus.Add((succeeded, refrenceModel.Filename));
            }
             model.CommitChanges();

            var refreshResult = new List<string>();

            if (!refreshStatus.Any())
            {
                refreshResult.Add("No reference models found");
                result.Result = ExecutionResult.PartiallySucceeded;
            }
            
            else if (refreshStatus.All(x => !x.Item1))
            {
                refreshResult.Add("No reference models were updated");
                result.Result = ExecutionResult.PartiallySucceeded;
            }

            foreach (var (succeeded, filename) in refreshStatus)
            {
                if (succeeded)
                {
                    refreshResult.Add($"{filename} was updated");
                }
                else
                {
                    refreshResult.Add($"{filename} was not updated");
                }
            }
            result.MyTextResult = $"{string.Join("\n", refreshResult)}";

            if(result.Result == ExecutionResult.Failed)
                return Result.Text.Failed(result.MyTextResult);
            else if(result.Result == ExecutionResult.PartiallySucceeded)
                return Result.Text.PartiallySucceeded(result.MyTextResult);
            
            return Result.Text.Succeeded(result.MyTextResult);
        
    }
}
    public class RefreshReferenceModelsResult 
    {
        public ExecutionResult Result { get; set; }
        public string MyTextResult { get; set; } = string.Empty;
    }