
using System;

namespace aoEmailBounceProcess {
    namespace Controllers {
        public static class genericController {
            //
            //====================================================================================================
            /// <summary>
            /// return true if the date is empty or null.
            /// </summary>
            /// <param name="srcDate"></param>
            /// <returns></returns>
            public static bool isDateEmpty(DateTime srcDate) {
                return (srcDate < new DateTime(1900, 1, 1));
            }
            //
            //====================================================================================================
            /// <summary>
            /// if date is invalid, set to minValue
            /// </summary>
            /// <param name="srcDate"></param>
            /// <returns></returns>
            public static DateTime encodeMinDate(DateTime srcDate) {
                if (isDateEmpty(srcDate)) { return DateTime.MinValue; }
                return srcDate;
            }
            //
            //====================================================================================================
            /// <summary>
            /// if valid date, return the short date, else return blank string 
            /// </summary>
            /// <param name="srcDate"></param>
            /// <returns></returns>
            public static string getShortDateString(DateTime srcDate) {
                if (!isDateEmpty(srcDate)) { return encodeMinDate(srcDate).ToShortDateString(); }
                return string.Empty; ;
            }
            //
            //====================================================================================================
            /// <summary>
            /// create a string for sort order -- convert integer to normalized string (7 digit numeric string)
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public static string getSortOrderFromInteger(int id) {
                return id.ToString().PadLeft(7, '0');
            }
            //
            //====================================================================================================
            /// <summary>
            /// create a string for html input requirement (yyyy-mm-dd format)
            /// </summary>
            /// <param name="source"></param>
            /// <returns></returns>
            public static string getDateForHtmlInput(DateTime source) {
                if (isDateEmpty(source)) {
                    return "";
                } else {
                    return source.Year + "-" + source.Month.ToString().PadLeft(2, '0') + "-" + source.Day.ToString().PadLeft(2, '0');
                }
            }
        }
    }
}
