using System.Collections.Generic;

namespace Cmas.Services.TimeSheets.Dtos.Responses.Rate
{
    /// <summary>
    ///  Ставки и потраченное время по дням
    /// </summary>
    public class RateResponse
    {
        /// <summary>
        /// Идентификатор ставки
        /// </summary>
        public string Id;

        /// <summary>
        /// Наименование ставки
        /// </summary>
        public string Name;

        /// <summary>
        /// Ед. изм
        /// </summary>
        public int Unit;

        /// <summary>
        /// Потраченное время
        /// </summary>
        public IList<double> SpentTime;

        public RateResponse()
        {
            SpentTime = new List<double>();
        }
    }
}