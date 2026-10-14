from SetWallParameterArgs import args
from assistant.integrations.revit import revit
from Autodesk.Revit.DB import Transaction, FilteredElementCollector, Wall, Document
from System import Guid

def get_first_wall(doc):
    # type: (Document) -> Wall

    return FilteredElementCollector(doc).OfClass(Wall).FirstElement()

doc = revit.doc

set_parameter = args.set_parameter.value
check_parameter = args.check_parameter.value
parameter_guid = Guid('c7bdd5fa-ee58-4290-a9f6-29e7cbefc175')
parameter_value = 'Hello World!'

wall = get_first_wall(doc)

if set_parameter:
    with Transaction(doc, 'Set wall parameter') as trans:
        trans.Start()
        wall.get_Parameter(parameter_guid).Set(parameter_value)
        trans.Commit()

if check_parameter:
    wall_parameter_value = wall.LookupParameter('drofus_occurrence_id').AsString()
    if wall_parameter_value != parameter_value:
        raise Exception('Wall parameter value is not correct!')
