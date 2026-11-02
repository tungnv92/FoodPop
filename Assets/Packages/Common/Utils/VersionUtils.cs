using System;
using UnityEngine;

public class VersionUtils 
{
    public static int CompareVersions(string v1, string v2)
    {
        Version version1 = new Version(v1);
        Version version2 = new Version(v2);
        return version1.CompareTo(version2);
    }
}
