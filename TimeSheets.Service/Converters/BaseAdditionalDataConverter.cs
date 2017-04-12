using System;
using Newtonsoft.Json;
using Cmas.Services.TimeSheets.Dtos.Responses.AdditionalData;

namespace Cmas.Services.TimeSheets.Converters
{
    public class BaseAdditionalDataConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(BaseAdditionalDataResponse);
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is DefaultAdditionalDataResponse)
                serializer.Serialize(writer, (value as DefaultAdditionalDataResponse));
            else if (value is SouthTambeyAdditionalDataResponse)
                serializer.Serialize(writer, (value as SouthTambeyAdditionalDataResponse));
            else
                throw new NotSupportedException();
        }
        
    }
}
