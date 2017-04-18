using System.Collections.Generic;

namespace Cmas.Services.TimeSheets.Dtos.Requests
{
    public class UpdateSpentTimesRequest
    {
        /// <summary>
        /// ID Ставки
        /// </summary>
        public string Id;

        /// <summary>
        /// Потраченное время в разрезе дней
        /// </summary>
        public IEnumerable<double> SpentTime;
    }
}
