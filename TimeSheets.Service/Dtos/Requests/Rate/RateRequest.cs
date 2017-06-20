using System.Collections.Generic;

namespace Cmas.Services.TimeSheets.Dtos.Requests.Rate
{
    public class RateRequest
    {
        /// <summary>
        /// Идентификатор ставки
        /// </summary>
        public string Id;
        
        /// <summary>
        /// Потраченное время
        /// </summary>
        public IList<double> SpentTime;

        public RateRequest()
        {
            SpentTime = new List<double>();
        }
    }
}
