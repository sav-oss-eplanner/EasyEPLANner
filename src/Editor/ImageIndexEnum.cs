using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor
{
    /// <summary>
    /// Перечисление с индексами картинок для полей.
    /// </summary>
    public enum ImageIndexEnum
    {
        TechObjectManager = 0,
        TechObject = 1,
        ModesManager = 2,
        Mode = 3,
        Step = 4,
        ActionON = 5,
        ActionOFF = 6,
        ActionSignals = 7,
        ActionWashDIDO = 10,
        ActionWashUpperSeats = 8,
        ActionWashLowerSeats = 9,
        ActionWash = 11,
        ActionDIDOPairs = 10,
        ParamsManager = 12,
        Equipment = 13,
        GenericTechObject = 14,
        GenericGroup = 15,
        Run = 16,
        STATE_IDLE = 17,
        STATE_STARTING = 18,
        STATE_RUN = 19,
        STATE_COMPLETING = 20,
        STATE_COMPLETE = 21,
        STATE_PAUSING = 22,
        STATE_PAUSE = 23,
        STATE_UNPAUSING = 24,
        STATE_STOPPING = 25,
        STATE_STOP = 26,
        NONE = 100,
    }
}
