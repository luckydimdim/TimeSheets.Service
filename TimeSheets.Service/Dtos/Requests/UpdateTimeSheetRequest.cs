using Cmas.Services.TimeSheets.Dtos.Requests.Rate;
using System;
using System.Collections.Generic;

namespace Cmas.Services.TimeSheets.Dtos.Requests
{
    public class UpdateTimeSheetRequest
    {
        /// <summary>
        /// Период табеля - начало
        /// </summary>
        public DateTime From;

        /// <summary>
        /// Период табеля - окончание
        /// </summary>
        public DateTime Till;

        /// <summary>
        /// Примечания
        /// </summary>
        public string Notes;

        /// <summary>
        /// Ставки
        /// </summary>
        public IList<RateRequest> Rates;
    }
}
