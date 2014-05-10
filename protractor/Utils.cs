﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP;
using UnityEngine;

class Utils
{
    public static int HexToInt(char hexChar)
    {
        //string hex = "" + hexChar;
        switch (hexChar)
        {
            case '0': return 0;
            case '1': return 1;
            case '2': return 2;
            case '3': return 3;
            case '4': return 4;
            case '5': return 5;
            case '6': return 6;
            case '7': return 7;
            case '8': return 8;
            case '9': return 9;
            case 'a': return 10;
            case 'b': return 11;
            case 'c': return 12;
            case 'd': return 13;
            case 'e': return 14;
            case 'f': return 15;
        }
        return -1;
    }

    public static Color hextorgb(string color)
    {
        float red = (float)((HexToInt(color[1]) + HexToInt(color[0]) * 16.000) / 255);
        float green = (float)((HexToInt(color[3]) + HexToInt(color[2]) * 16.000) / 255);
        float blue = (float)((HexToInt(color[5]) + HexToInt(color[4]) * 16.000) / 255);
        var finalColor = new Color();
        finalColor.r = red;
        finalColor.g = green;
        finalColor.b = blue;
        finalColor.a = 1;
        return finalColor;
    }

    public static string removeseconds(string str)
    {
        int i=0;

        for (int x = 0; x < str.Length; x++)
        {
            if (str[x] == ':') i = x;
        }

        return str.Substring(0,i);
    }

    public static string fromSeconds(double seconds)
    {
        if (GameSettings.KERBIN_TIME)
        {
            double s = seconds;

            int days = (int)(s / (60*60*6));
            s -= days * (60*60*6);
            int hours = (int)(s / (60*60));
            s -= hours * (60*60);
            int minutes = (int)(s / 60);

            return String.Format("{0}.{1}:{2}:{3}", days, hours, minutes, s);
        }
        else
        {
            return TimeSpan.FromSeconds(seconds).ToString();
        }
    }
}
