using CW.Assistant.Extensions.Contracts.Fields;

namespace TeklaReadIn;

public class TeklaReadInArgs
{
        [BooleanField(Label = "Save model after Read in", ToolTip = "Save model after Read in is done, if there are any conflicts it won't save")]
        public bool Save { get; set; } = false;

        [BooleanField(Label = "Fail Task on Conflicts", ToolTip = "Fail task if there are any conflicts after read in")]
        public bool FailTask { get; set; } = false;
}