﻿namespace Registry.Adapters.DroneDB;

public enum DDBError
{
    DDBERR_NONE = 0, // No error
    DDBERR_EXCEPTION = 1, // Generic app exception
    DDBERR_BUILDDEPMISSING = 2
};