namespace Cmas.Services.TimeSheets.Dtos.Responses.AdditionalData
{
    class DefaultAdditionalDataResponse : BaseAdditionalDataResponse
    {
        /// <summary>
        /// Тип данных
        /// </summary>
        public override string Type
        {
            get { return "default"; }
        }
    }
}