using System;
using System.Collections.Generic;
using UnityEngine;

public static class DateTimeExt
{
    public static DateTime GetStartOfNextDay(this DateTime dateTime)
    {
        return dateTime.AddDays(1);
    }
    public static DateTime SetTime(this DateTime dateTime, int hour, int minute, int second)
    {
        return new DateTime
        (
            year: dateTime.Year,
            month: dateTime.Month,
            day: dateTime.Day,
            hour: hour,
            minute: minute,
            second: second
        );
    }
    public static string GetSeason(this DateTime dateTime)
    {
        int month = dateTime.Month;
        if (month >= 3 && month <= 5)
        {
            return "spring";
        }
        else if (month >= 6 && month <= 8)
        {
            return "summer";
        }
        else if (month >= 9 && month <= 11)
        {
            return "fall";
        }
        else
        {
            return "winter";
        }
    }
}