using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace PrintPDF.Enums;

public enum NamingOptions
{
    [Description("Sheet Name Only")]
    SheetNameOnly,
    [Description("Sheet Number Only")]
    SheetNumberOnly,
    [Description("Sheet Name - Sheet Number")]
    SheetNameAndNumber,
    [Description("Sheet Number - Sheet Name")]
    SheetNumberAndName,
    [Description("Project Name - Sheet Name - Sheet Number")]
    ProjectNameAndSheetNameAndNumber,
    [Description("Project Name - Sheet Name")]
    ProjectNameAndSheetName,
    [Description("Project Name - Sheet Number")]
    ProjectNameAndSheetNumber,
    [Description("Sheet Name - Sheet Number - Project Name")]
    SheetNameAndNumberAndProjectName,
    [Description("Sheet Name - Project Name")]
    SheetNameAndProjectName,
    [Description("Sheet Number - Project Name")]
    SheetNumberAndProjectName,
    [Description("Custom Naming Convention")]
    CustomNamingConvention
}
