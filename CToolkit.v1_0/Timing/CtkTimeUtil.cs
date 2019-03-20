using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CToolkit.v1_0.Timing
{
    public class CtkTimeUtil
    {
        //ToUniversalTime/ToLocalTime �|�۰ʧP�OKind = Local / Utc �ӨM�w�[��
        //�Y��Unspecified, �h�i�����,
        // toLocal: +8 & Kink = Local
        // toUniversal: -8 & Kind = Utc



        //--- DateTime and Timestamp converter ---------

        //--- ROC ---------
        const int RocYearToYear = 1911;



        public static int QuarterOfYear(DateTime dt) { return (dt.Month - 1) / 3 + 1; }


        #region String to DateTime

        public static DateTime DateTimeParseExact(string s, string format = "yyyyMMdd") { return DateTime.ParseExact(s, format, CultureInfo.InvariantCulture); }
        public static DateTime DateTimeParseExact(string s, DateTime defaultDt, string format = "yyyyMMdd")
        {
            var dt = defaultDt;
            DateTimeTryParseExact(s, out dt);
            return dt;
        }

        public static bool DateTimeTryParseExact(string s, out DateTime result, string format = "yyyyMMdd") { return DateTime.TryParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result); }

        public static DateTime FromYyyy(string s) { return DateTimeParseExact(s, "yyyy"); }
        public static DateTime FromYyyyMm(string s) { return DateTimeParseExact(s, "yyyyMM"); }
        public static DateTime FromYyyyMmDd(string s) { return DateTimeParseExact(s, "yyyyMMdd"); }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="yyyyqq"></param>
        /// <returns>�өu�Ĥ@��</returns>
        public static DateTime FromYyyyQq(string yyyyqq)
        {
            var yyyy = Convert.ToInt32(yyyyqq.Substring(0, 4));
            var qq = Convert.ToInt32(yyyyqq.Substring(4));

            var date = new DateTime(yyyy, 1, 1);
            date = date.AddMonths((qq - 1) * 3);

            var realYyyyQq = GetYyyyQq(date);
            if (yyyyqq != realYyyyQq) throw new InvalidOperationException();

            return date;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="yyyyww"></param>
        /// <returns>�өP���Y��</returns>
        public static DateTime FromYyyyWw(string yyyyww)
        {
            var yyyy = Convert.ToInt32(yyyyww.Substring(0, 4));
            var ww = Convert.ToInt32(yyyyww.Substring(4));

            var date = new DateTime(yyyy, 1, 1);
            date = date.AddDays(7 * ww - 7);

            var realYyyyww = GetYyyyWw(date);
            if (yyyyww != realYyyyww) throw new InvalidOperationException();

            return date;
        }

        #endregion

        #region Week Operation
        //--- Week ---------





        /// <summary>
        /// ���W�L���e����� dow(Day Of Week) (e.q.�P�G) �O����
        /// </summary>
        /// <param name="dow"></param>
        /// <returns></returns>
        public static DateTime GetLastDow(DayOfWeek dow) { return GetLastDow(dow, DateTime.Now); }

        public static DateTime GetLastDow(DayOfWeek dow, DateTime date)
        {

            var rs = date.AddDays((int)dow - (int)date.DayOfWeek);

            //�p�G�W�L���e���, �N�⥦��^��
            if (rs > date)
                rs = rs.AddDays(-7);

            return rs;
        }

        public static DateTime GetThisDow(DayOfWeek dow) { return GetThisDow(dow, DateTime.Now); }

        public static DateTime GetThisDow(DayOfWeek dow, DateTime date) { return date.AddDays((int)dow - (int)date.DayOfWeek); }

        public static DateTime GetWeeklyEnd(DateTime date)
        {
            var last = GetThisDow(DayOfWeek.Saturday, date);
            return last;
        }

        public static DateTime GetWeeklyEndInSameYear(DateTime date)
        {
            var last = GetWeeklyEnd(date);
            if (last.Year > date.Year) last = new DateTime(date.Year, 12, 31);
            return last;
        }

        public static DateTime GetWeeklyStart(DateTime date)
        {
            var first = GetThisDow(DayOfWeek.Sunday, date);
            return first;
        }

        public static DateTime GetWeeklyStartInSameYear(DateTime date)
        {
            var first = GetWeeklyStart(date);
            if (first.Year < date.Year) first = new DateTime(date.Year, 1, 1);
            return first;
        }

        public static int GetWeekOfYear(DateTime date)
        {
            return CultureInfo
               .InvariantCulture
               .Calendar
               .GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        }

        #endregion

        #region DateTime to String

        public static string GetYyyy(DateTime dt) { return dt.ToString("yyyy"); }
        public static string GetYyyyMm(DateTime dt) { return dt.ToString("yyyyMM"); }
        public static string GetYyyyMmDd(DateTime dt) { return dt.ToString("yyyyMMdd"); }
        public static string GetYyyyQq(DateTime dt)
        {
            var qq = QuarterOfYear(dt);
            return string.Format("{0}{1:00}", dt.ToString("yyyy"), qq);
        }
        public static string GetYyyyWw(DateTime dt)
        {
            var weekOfYear = CtkTimeUtil.GetWeekOfYear(dt);
            return string.Format("{0}{1:00}", dt.ToString("yyyy"), weekOfYear);
        }

        #endregion 



        #region Linux Timestamp

        public static DateTime ToDateTimeFromMilliTimestamp(double timestamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds(timestamp);
        }

        public static DateTime ToDateTimeFromTimestamp(double timestamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(timestamp);
        }

        public static DateTime ToLocalDateTimeFromTimestamp(double timestamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(timestamp).ToLocalTime();
        }


        public static Int64 ToMilliTimestamp()
        {
            return ToMilliTimestamp(DateTime.Now);
        }
        public static Int64 ToMilliTimestamp(DateTime dt)
        {
            return (Int64)(dt - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
        }

        public static double ToTimestamp()
        {
            return ToTimestamp(DateTime.Now);
        }
        public static double ToTimestamp(DateTime dt)
        {
            return (dt - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }

        public static Int64 ToUtcMilliTimestamp(DateTime dt)
        {
            return (Int64)(dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
        }

        public static double ToUtcTimestamp()
        {
            return ToUtcTimestamp(DateTime.Now);
        }
        public static double ToUtcTimestamp(DateTime dt)
        {
            return (dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }

        #endregion

        #region ROC DateTime

        public static DateTime ToDateTimeFromRoc(DateTime dt) { return dt.AddYears(RocYearToYear); }

        public static DateTime ToRocDateTime(DateTime dt) { return dt.AddYears(-RocYearToYear); }

        public static int ToYearFromRoc(int year) { return year + RocYearToYear; }

        #endregion


    }
}