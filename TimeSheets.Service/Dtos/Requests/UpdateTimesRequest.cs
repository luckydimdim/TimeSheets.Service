using System.Collections.Generic;
using Cmas.Services.TimeSheets.Converters;
using Newtonsoft.Json;

namespace Cmas.Services.TimeSheets.Dtos.Requests
{
    public class UpdateTimesRequest
    {
        public string RateId;
        public IEnumerable<double> SpentTime;
    }
}
