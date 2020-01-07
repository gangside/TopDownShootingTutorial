using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UndoRedo {

    static List<State> states = new List<State>();
    static int recordIndex = 0;

    public static void RecordState(object[] recordObjects)
    {
        State newState = new State();
        newState.records = recordObjects;
        states.RemoveRange(recordIndex + 1, states.Count - recordIndex - 1);
        states.Add(newState);
        recordIndex = states.Count - 1;
    }

    public static State Undo ()
    {
        if (recordIndex > 0)
        {
            recordIndex--;
        }
        return states[recordIndex];
    }

    public static State Redo()
    {
        if(recordIndex< states.Count - 1)
        {
            recordIndex++;
        }
        return states[recordIndex];
    }
}

public class State
{
    public object[] records;
}