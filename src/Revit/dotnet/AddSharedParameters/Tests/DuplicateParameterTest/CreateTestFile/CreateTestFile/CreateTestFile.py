from assistant.integrations.revit import revit
from Autodesk.Revit.DB import UnitSystem, ElementTypeGroup, FilteredElementCollector, Level, Line, XYZ, Wall, Transaction
from System import Guid, IO

uiapp = revit.uiapp
app = uiapp.Application

doc = app.NewProjectDocument(UnitSystem.Metric)

docuement_path = IO.Path.Combine(IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".rvt")
doc.SaveAs(docuement_path)

uiapp.OpenAndActivateDocument(docuement_path)

# get first level in doc with filtered element collector
level = FilteredElementCollector(doc).OfClass(Level).FirstElement()


# Create wall in document with first wall type
wall_type = doc.GetElement(doc.GetDefaultElementTypeId(ElementTypeGroup.WallType))

trans = Transaction(doc, "Create Wall")
trans.Start()
wall = Wall.Create(doc, Line.CreateBound(XYZ(0, 0, 0), XYZ(10, 0, 0)), level.Id, False)
trans.Commit()
