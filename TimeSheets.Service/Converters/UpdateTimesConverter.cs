using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Cmas.Services.TimeSheets.Dtos.Requests;

namespace Cmas.Services.TimeSheets.Converters
{
    public class UpdateTimesConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(UpdateSpentTimesRequest);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            IDictionary<string, IEnumerable<double>> result;

            if (objectType != typeof(UpdateSpentTimesRequest))
                throw new ArgumentException("Unsupported type: " + objectType.ToString());

            result =
                (IDictionary<string, IEnumerable<double>>)
                    serializer.Deserialize(reader, typeof(IDictionary<string, IEnumerable<double>>));
 
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}