using AddSharedParameters.Extensions;

namespace AddSharedParameters.Helpers;

public class CategoryHelper
{
    public Category? GetCategoryIfParametersCanBindToIt(Document document, string categoryName)
    {
        if (!Enum.TryParse<BuiltInCategory>(categoryName, out var builtInCategory))
            return null;

        var categories = document.Settings.Categories;

        foreach (Category category in categories)
        {
            if (category.IsSameAs(builtInCategory) && category.AllowsBoundParameters) 
                return category;
        }

        return null;
    }

    public CategoriesToBindResult CollectCategoriesToBind(Document document, CategorySet categorySet, AddSharedParametersArgs args)
    {
        // Create a copy of the CategorySet to avoid disposing the original
        var categoriesCopy = document.Application.Create.NewCategorySet();
        foreach (Category category in categorySet)
        {
            categoriesCopy.Insert(category);
        }

        var result = new CategoriesToBindResult(categoriesCopy);

        var initialCategories = new List<Category>();
        foreach (Category category in categoriesCopy)
        {
            if (category.AllowsBoundParameters == false)
                continue;

            initialCategories.Add(category);
        }

        if (args.ResetCategories)
            result.CategorySet.Clear();

        foreach (var categoryName in args.GetNormalizedCategoryNames())
        {
            var category = GetCategoryIfParametersCanBindToIt(document, categoryName);
            if (category is null) continue;

            if (!result.CategorySet.Contains(category))
            {
                result.CategorySet.Insert(category);
            }
        }

        foreach (var categoryName in args.GetNormalizedCategoryNamesToRemove())
        {
            var category = GetCategoryIfParametersCanBindToIt(document, categoryName);
            if (category is null) continue;

            if (result.CategorySet.Contains(category))
            {
                result.CategorySet.Erase(category);
            }
        }

        CheckForNotValidCategories(document, result.CategorySet, result);

        result.HasChanges = CheckForChangedCategorie(initialCategories, result.CategorySet);

        return result;
    }

    private bool CheckForChangedCategorie(List<Category> initialCategories, CategorySet categorySet)
    {
        // Build HashSet for O(1) lookup instead of O(n) per iteration
        var initialCategoryIds = new HashSet<ElementId>(initialCategories.Select(c => c.Id));

        // Collect categories first to avoid modifying during iteration
        var currentCategories = new List<Category>();
        foreach (Category category in categorySet)
        {
            currentCategories.Add(category);
        }

        // Now check for changes
        foreach (var category in currentCategories)
        {
            if (!initialCategoryIds.Contains(category.Id))
                return true;
        }

        return false;
    }

    private void CheckForNotValidCategories(Document document, CategorySet bindCategories, CategoriesToBindResult result)
    {
        // Collect invalid categories first to avoid modifying during iteration
        var categoriesToRemove = new List<Category>();

        foreach (Category item in bindCategories)
        {
            try
            {
                var name = item.Name;

                if (name is null || !document.Settings.Categories.Contains(item.Name))
                {
                    categoriesToRemove.Add(item);
                }
            }
            catch
            {
                categoriesToRemove.Add(item);
            }
        }

        // Now remove invalid categories after iteration is complete
        foreach (var item in categoriesToRemove)
        {
            bindCategories.Erase(item);
        }

        if (categoriesToRemove.Count > 0)
        {
            result.HasUnValidCategory = true;
        }
    }
}
