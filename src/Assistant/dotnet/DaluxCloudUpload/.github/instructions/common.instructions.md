---
applyTo: '**/*.cs'
---
# Assistant Variables 
 
Write: `context.SetVariableValue("VariableName", value);`
 
Read: `string? v = context.GetVariableValue("VariableName");` (returns `string?`, handle `null`)