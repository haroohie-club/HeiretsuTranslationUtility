using HaruhiHeiretsuLib.Strings;
using System;
using System.Collections.Generic;

namespace HaruhiHeiretsuLib.Data;

/// <summary>
/// Representation 
/// </summary>
public class SystemMessagesFile : DataFile, IDataStringsFile
{
    /// <inheritdoc/>
    public List<DialogueLine> GetDialogueLines()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ReplaceDialogueLine(DialogueLine line)
    {
        throw new NotImplementedException();
    }
}