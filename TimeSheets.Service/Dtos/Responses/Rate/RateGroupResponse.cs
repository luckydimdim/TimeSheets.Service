using System.Collections.Generic;

namespace Cmas.Services.TimeSheets.Dtos.Responses.Rate
{
    /// <summary>
    ///  Группа ставок
    /// </summary>
    public class RateGroupResponse
    {
        /// <summary>
        /// Наименование группы
        /// </summary>
        public string Name;

        /// <summary>
        /// Ставки в группе
        /// </summary>
        public IList<RateResponse> Rates;

        public RateGroupResponse()
        {
            Rates = new List<RateResponse>();
        }
    }
}
