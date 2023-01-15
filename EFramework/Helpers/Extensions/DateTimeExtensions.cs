using System;

namespace Spolis.Helpers
{
    public static class DateTimeFunctions
    {
        /// <summary>
        /// DatumsLielaks var norādīt kā DateTime.MinValue, tiks atgriezts null
        /// </summary>
        /// <param name="DatumsMazaks"></param>
        /// <param name="DatumsLielaks"></param>
        /// <returns></returns>
        public static int? Year(DateTime DatumsMazaks, DateTime DatumsLielaks)
        {
            if (DatumsLielaks == DateTime.MinValue)
                return null;
            if (DatumsMazaks.Month <= DatumsLielaks.Month)
            {
                if (DatumsMazaks.Month < DatumsLielaks.Month)
                {
                    return DatumsLielaks.Year - DatumsMazaks.Year;
                }

                if (DatumsMazaks.Day <= DatumsLielaks.Day)
                {
                    return DatumsLielaks.Year - DatumsMazaks.Year;
                }

                if (DatumsMazaks.Day > DatumsLielaks.Day)
                {
                    return (DatumsLielaks.Year - DatumsMazaks.Year) - 1;
                }

            }
            return (DatumsLielaks.Year - DatumsMazaks.Year) - 1;
        }

    }
}
