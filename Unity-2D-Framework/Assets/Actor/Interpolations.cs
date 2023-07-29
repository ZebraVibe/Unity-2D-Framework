using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// takes in a linear value betweeen 0-1 and outputs a possibliy non linear integer value 
/// roughly in the range of 0 and 1
/// [Note] this must be followed: x=0 -> y=0 and x=1 -> y=1
/// 
/// Author: Sebastian Krahnke
/// </summary>
public delegate float Interpolation(float a);

public class Interpolations
{
    private Interpolations() { }

    public static Interpolation Linear = a =>
    {
        return a;
    };

    public static Interpolation Smooth = a =>
    {
            return a*a *(3-2*a);
    };
    

    public static Interpolation Smooth2 = a =>
    {
            a = a * a * (3 - 2 * a);
            return a * a * (3 - 2 * a);
    };

    public static Interpolation Pow2 = a =>
    {
        return a * a;
    };

    public static Interpolation Pow4 = a =>
    {
        return a * a * a * a;
    };

    public static Interpolation Sin = a =>
    {
        if (a < 0.5f) return SinIn(a * 2);
        else return SinOut((a - 0.5f) * 2);
    };

    public static Interpolation SinIn = a =>
    {
        return Mathf.Sin(a * Mathf.PI /2f);
    };

    public static Interpolation SinOut = a =>
    {
        return 1 + Mathf.Sin((a + 3) * Mathf.PI / 2f);
    };

}
